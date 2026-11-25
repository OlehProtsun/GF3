"""SQLite migration rehearsal and maintenance recovery primitives."""

from contextlib import closing
from dataclasses import dataclass
import base64
import hashlib
import json
import os
from pathlib import Path
import re
import secrets
import shutil
import sqlite3
import stat
import tempfile
import time
import zlib


MIGRATIONS = "DataAccessLayer/Migrations"
PAIR = re.compile(r"([0-9]{14}_[A-Za-z][A-Za-z0-9_]*)(\.Designer)?\.cs")
RELATED = {
    f"{MIGRATIONS}/AppDbContextModelSnapshot.cs",
    "DataAccessLayer/Models/DataBaseContext/AppDbContext.cs",
    "DataAccessLayer/Models/ShiftCorrectionRequestModel.cs",
    "DataAccessLayer/Models/ShiftSwapRequestModel.cs",
    "GF3.Tests/DatabaseMigrationStartupTests.cs",
}
BOOTSTRAP_PATHS = {
    "GF3.WebApi/Program.cs",
    "deploy/security/gf3_prepare_release.py",
    "deploy/security/gf3_release.py",
    "deploy/security/gf3_maintenance.py",
    "deploy/security/gfctl-prod2-maintenance-menu.patch",
}
REVIEWED_BUILD_INPUTS = {
    ".github/workflows/ci.yml", "Dockerfile", "GF3.Tests/GF3.Tests.csproj", "GF3.WebApi/WebApi.csproj",
    "GF3.WebApi/Infrastructure/BrowserTestStartup.cs", "GF3.WebApi/Infrastructure/EmployeeWebPushHostedService.cs",
    "GF3.WebApi/Infrastructure/WebApiServiceCollectionExtensions.cs",
    "DataAccessLayer/Administration/SqliteDatabaseWorkspace.cs", "DataAccessLayer/Administration/SqliteMediaBackup.cs",
    "GF3.Tests/DatabaseMigrationStartupTests.cs", "deploy/security/tests/rehearse_release.py",
}
IDENTIFIER = re.compile(r"[A-Za-z_][A-Za-z0-9_]*")
HISTORY = "__EFMigrationsHistory"
LOCK = "__EFMigrationsLock"
RELEASE_BASELINE = "1175c39461e149a7bb35f428babed28279727899"
REVIEWED_RELEASE_MIGRATIONS = (
    "20261122150944_AllowDisjointOpenSwapIntervals",
    "20261122151455_TrackEmployeeLaunchModes",
    "20261122160000_AddWorkflowLogLaunchMode",
    "20261122173000_AddShiftRemovalCorrections",
    "20261123120000_AddCommunicationGifAttachment",
    "20261123130000_AddCommunicationPhotoAttachment",
    "20261123140000_AddCommunicationContentBlocks",
)
REVIEWED_RELEASE_BLOBS = {
    REVIEWED_RELEASE_MIGRATIONS[0]: ("69a6702bf1ab39c556824be43ff04b4eae0b3201", "4a2d4064f33ac782424721f315dce3579c9cbd62"),
    REVIEWED_RELEASE_MIGRATIONS[1]: ("3575cbe54a460187627a9bf9fba58d95b2b3f37e", "bd1d0b9d8f3bf9dab455ca25e689724d29d4ebed"),
    REVIEWED_RELEASE_MIGRATIONS[2]: ("3545a85d22ea23a10b673d8f6a23075209ad0bd1", "21820f0e9b7e5fd0e8e6e21d67295c9baf64e357"),
    REVIEWED_RELEASE_MIGRATIONS[3]: ("1b3fb5bd3a53ae8493e16c55ac8b1a8e1261d4f8", "1f20096a7d8eed79abaaf21b63df35c53b5c9cd8"),
    REVIEWED_RELEASE_MIGRATIONS[4]: ("6ad55b3eeb6859b05cba83b450378c3d1cf32a1e", "2b6e2e6a6275285b096ea74c0e0fe10013f4fa78"),
    REVIEWED_RELEASE_MIGRATIONS[5]: ("a1d10413b5d22af1912a9181f05cd6e104c49542", "f8c2cf46235bd16cd4d58b5c41be4b8a398dc10a"),
    REVIEWED_RELEASE_MIGRATIONS[6]: ("e55adb130378e8a5d90d0eb714873bf341d99b08", "4a13f63aa97fb6bc3b637cf61e8aac196c755d0a"),
}
REVIEWED_SCHEMA_PREFIX_CONTRACTS = json.loads(zlib.decompress(base64.b85decode(
    b'c-rlpZEw@a7J&bXl`jR!E9I@cY44Rv2y8`3xNQ)-Vo~HKZtEq9b8UxGt?qxnV>@>IGGmX^LV<nwK&yJ@%$b*S=6KHd`S5pZ>0EkN'
    b';JB;fdFxrL+wMN<bUKHf-r?ik;q=tL@NEC;jkVfXi?b`|A{bv>*j~$Q`Lio~X^qeSvS)$cdiLQ%%UR9sFXGAZ@6(yPTy9p*ERs82'
    b'+P-gH+S8e5TY){Fp5IKD)=K=vn>uq*L0Ve;eQ-KFnhcHO(Tm~l#-9!4{@F6dBT-9QMLc{U9?9!HY<)Cyz460-i?%&a^fi>?+5g?x'
    b'ejt8+cI<0Ok5o0JooXmP>6ofo+&5KSMd$o$Yn`@h&1Uv`J23Wgy>M@AJKL{p;ffyV$jo;pTO;m<vYJ*u)@#rGEGow}m-Z@{+FwF}'
    b';c&%It;MZ-rrSzwU-bp$d+&UyHVl0etCQ5gsH|YR31-@=&TVVHa8~vm=$eSqP&O&rRNK#v@0>5}>4oPm??B_MI1R0{#T9CooI`3n'
    b'eqaR~zmbp9kf-OAau#fc#kZB~^$)k3`DSrv`WNKs>7Q28)IYNEXqF0ESNj=d{W=d46>U1mTQR}LQ;%{JedVI+R4>+QqWoQG-B8`V'
    b'^b9QT(hhE;Wlln!j@!ce>*3n<K3y!_>*>P1oVv5wMp!}U$i>s#Be5}gH&Rtq5)xI*Vq#S@MHLP$SJQLF$Oq2S-cz%*ew|*{EO5Q3'
    b'csz79dTze#Mq5NK?q;<ce(|{YY^UhE(eZ!Y?ewjZlc-!QAn~yE%58kxTUpCnE0@TrR&H0Sq*=-DuI<&_SzS)&*3HztxX|+VWku`6'
    b'?&<bRCKZdDF5OjdC3<JP|800WG-5+D{$(7zjhj6%jz%vG*K;nNm5AgbcO5Q6<M_-Ny*oM49u<G#26sIqq0ENk5mqU?mFnIXD@T1F'
    b'Pb&H!c{W$5nQkrF-pk#ms^3ysP4_0vZjVJXai5b{g-@by;gjK)*_E}rw3DY&@l6KiPDVs$DE1nINc+GNA!|jfToXf2j-H<kE5l;%'
    b'&y^wm32z_1rW}tZ!&k%8bOepTcyu;7JraKxMc%%issz0~J$`d^`iJp)_=j=yZZbX|2@T#1M-y|`Lf{19j$llNzfaN@GSAQ5`Ax;+'
    b'(p!hJWbtLHwqWt3ARUI<;#>EfZ@`efY;4P9CFTZq_DS)YvmKVfZ^OZB<H5pST?SVV0?%1K2%kKBX#Cq~KWr#7^Rl;<TNqs-G(U<>'
    b'TSz8bqIUGCQty|Fg_aI`vV*E7@a6b)c>HP<Eys!@E8}$dQdr(-Fgz2j7KF#98<!K-{bG1B6c%+hj3+VF`(e&RC82;iI!Fvty5%>t'
    b'Nr?;{bIhM76<f&^TnMMTiOoE>&c%?tJbHIBF%H&?gIsyxv_(khtcn&`lq<^lwp~+fZTY^4H0RSR%fG5H=Cqt=`*yeu&+UAb%HsBy'
    b'wIhDhR3)yn_3>p-qo}o6&-MJ0FP3|_>_6LH__KO?GErnCZ!kVN376PtJUBWV=Dd>i*>V=vxwCMBn<#{uuA`p#)4%+vnHnGU^0hj`'
    b'E}TymEHEEo=j|>Nqe~1<bYgJyVpz7fiZHG|!b|M0GGuFLhZVAP>`{By%7@KWOGDel%fYo`EvE$ox2o9AGRgwS6{U+(Wo5S~^pz&3'
    b'%mFBN_T0Nl)j*XaIi<+tv_auBlknc$hG%x<xa)(2(kh!rr=Q6-o>?2l^Wo(Ea5yqLQNY~mJk%Gun-bx;Y;V_qWM++DjnWJ@Xe#nU'
    b'7p}LA4m`HaJb0jf-^;%aZW~M<3fFOJs46FupcIfTP<pcv@k;HxnCeya)yep|aqyx2OZTJr@9D>9;{U_=SLN&Nuf{>Q|E%3Ukn5{2'
    b'GO3XY6v0>K1uHj~wNm8^AggI<F*Ffc)>XL`DKmiIXiI9YSTTH6x~YPy%CzJy0@&u-@78_JTNZT9JuE1gJlkuhFT9{vy|w3e_tN|L'
    b'Ui*4Miq4LnreQ1hDs_%mv9nl<@XU2>d4VI0JFaf6X11&^$#&Du)^Ct{RjJ_3uAERrxGpQAN8#~9%@&KgLC_9<%E=m)j*g$&$}SJ9'
    b'H9*Vg-i9!N#b&vxY{Lz&bx5Nec{#19DLU{{2M({qz#rslMF(MZD#%hrs%|Nd?rJJjckdp8tmq&udjus(?x<u(M>x@|QbiEAIbS%l'
    b'D2DUmJ3rYqEEOzwx4F9!GZDRc)-2dQOD%GryAuWP+~l@I-gnn1C3oqkL($jc!K6v=vu~IR$5`x?^14ftUHw+NO59RG$#RKrr=4`!'
    b'7xa^r`RW>eqmW`hl|ttuaYZWS(h>>{lT1OA$VZx{t;*ddk&hxZ+c5HxO3{gYN*Q0}exA)0*A&tVj-sN{WBFVmClj%Zb4R)#&3O33'
    b'>zRLVXX05!0wxR+qN|*3vlU&6+iKP_JLYP$Sco5OF{d3fPJoC4VXNuWcJ#DsM(Gdv!=718e#p!9%|aX{FLr3QRCh{K)!W$$k4#nS'
    b'L*Bq+vy}fx3uXk1ffld*wsX09ZQpE1P>f;WEvBW`Hu`TgHjW09<MBxRp1j*ce;y5^xs39E+sX9YYw@$KeML=_6q<`J<)>NNKT-Di'
    b'@kkhk7>sDTe@VwAeIWY$KQmFIqjj%Op@M8lG}fTo{MNBT9rcoGtW;0uW~M?7i2>-gA8C*nyCiSZ#Ln}#e~ZOZgHQ4*86#?isg%~v'
    b'>`SQ>nH@!jUreUR3yOKlrBmeg`^Z!#Q{-YFo7L$Qd95dAT|z}(@2ROusmQz25ffI(s3@nUBjzA;y+<ZZ%v^OIrGc-LU2~GVwrz+|'
    b'>)ERBD|O^{lkasqY$MDaHSlEBceqr|7J9vxtybiEn%Jz)#wlh#d%mt*L_IwhAgs20>Z`Qd;%TnDpfFv@0j+YIE}gr|OFl7+c^9Q+'
    b'DnIU!hZd1-?!x;@yDUc9YN2AH((9r-*P3Kp693xinMvlQfypLXw2F$?nryhvr16b%%<2Q2a8Z6OKT>m#E;j0p%$JmBs$|;PIVwEt'
    b'YC4?y+m9+0i;J$LU9Z|Rb<L71D3s{w1YcS=_7$N{eIIT$!oB@I;Zi7D;TIamhkea2HPnG~^`5iOut_$gLSmxHR#ZqE$X#vMY^gsu'
    b'le<OHbyeyhuTHDo+HFxyJ_nPxD>g4lzCgKu6*C6PkW**N7CTq*;7zHgSf%Bhfofzru_w>v3Dg^ss98!C$omy8ze2V^QLty`5(c@g'
    b'F3J(4s`kxNl0YHz(3Io})ZKbys!|2&I*-jlwm_lE6V1TjX@kI^+m;3f@papzOn6XJItx{c?3$TOK|bDylLM*XXu|SI0)^O`&?CLL'
    b'T<*7S5OSL+rWkQZN2??!IUddk>J<b@ELBHLzaU2K7j>i^MmtA!vZK|^E!H<%8-*1wt*auNr&_^R)(&nJd?`LMP-$1?FYZ>!Zp~L@'
    b'is(RP{CBHdul7?sRnYgOF6g_JUVYu~8&&ElwvW5j^6(BmTvwkTHWSh;bbZtq9iUpK6;7_PR*x$`y;W!+jjQfto^!FZG}YB4mM$cs'
    b'l;=9TG?X$0Q%XW9V}ib!Nkb`fFmVz}_PDhDSEMmiUwBrnoLyJTZ$FYD+h>QU{4TbwJPxdDyW&VNYF|cW>2^DZ?Wg_z^eBA!dExkf'
    b'xy~vGW4<%7+K*R(?FozafwKT-0nP%P1vm?E7T_#D0i31bx9@UIfAEjqoxMJKuNaE2`?Wii9Vk0ccA)G)*@3e2T~c<yv4CR%#{!N8'
    b'91A!Wa4g_hz_Ea10mlN41sn@F7H}-!SirG>V*$qkjs+YGI2Ld$;8?)1fMWs20*(b73pf^VEZ|tcv4CR%#{!N891A!Wa4g_hz_Ea1'
    b'0mlN41sux{fn!mAPSw6w982AIZ*6ca;8?)1fMWs20*>W-z_E1thlkUN*wH@?xAS*mZC10ZH}2d9!~%!~5DOp{KrDb*0I>jK0mK4`'
    b'1rQ4$7C<b3SOBpAVgbYghy@S}AQnI@fLQJU#FEU<x#wPs2BwSX;9Azvq~!YAswf561+WWX7r-unT>!fPb^+`H*aff)U>CqHfL#E)'
    b'0CoZF0@wwx3t$((E`VJCy8w0p>;l-O66_Lxh^x-fDC#5=Tgue{j{zP7JO+3S@EG7Rz+-^N0FMD413U(J4DcA>F~DPh#{iE39s@iE'
    b'cnt8EZv~He)E57yNAvmntxhNI<&GW$JO+3S@EG7Rz+-^N0FMD413U(J4DcA>F~DPh#{iE39s@iEcnt6u;4#2sfX4uj0UiT926znc'
    b'7~nC$V}Qp1j{zP7JO+3S@EG7Rz+-^N0FMD413czCgvZ3EW6`15(w*maGPp8uW#G!dm4Pb*R|c*OTp74BaAn}iz?Fe316KyF3|tww'
    b'GH_+!%D|O@D+5;st_)lmxH52Men_rN6b5IKU|OdWuxD<^o_XBcvS-dhOE|Tcp`8!X7fc`e4D=c3Gtg(C&p@AnJ_CIQ`V90L=rhn~'
    b'pwB>`fj$F$2Ko&28R#?6XQ0nOpMgFDeWo#erUF57&up5t9VblG9%T@nK}^IF^$)%()h5cyO#PL$_OC<;R`K0?6#++=e)F@FZ~ozk'
    b'n{;7^fqs<nv!mo+a+W4pZ1s>DDI=#DLPv>tkw2FMc(Q%VnrBVl67leV?uq|X?2CM!<t4sniVzqfFhXF2zzBg60wV-Q2#gRIAuvK<'
    b'gun=a5dtFwMhJ`$7$Go1V1&R3{ooiOHBTmT2s)d0PZKlL*K<Q5J19v8<or-JE702c1zjf5V_hauL=TeW8!8w=YJyRT9;8MXl@JVC'
    b'45QHan?j6wnyi9;6Nu5F2E+(P(SD4gz8<6K@E#dOy}D1m`*n<>-n}!5YWr}j5x!eSQI*|)Ye-SQ7E*KwqzFh6kRl*OK#G7A0Vx7f'
    b'1f&Q^5s)GvML>#vUPw`|(`|1dMT2be<dt)A6a?1nYJ2nurwC3FoFX_yaEjm*!6|}M1g8j25u748MR1DX6u~KiQv|2zr{olg=a<ff'
    b'P%k+OQh(0}$_SJZC?iltpo~BnfiePR1j-1M5hx>2Mxcy98G$kaWdzCylo2Q+P)4APKpBBD0%Zis2$a!39Ay-JrYXs<fjt6y1ojB*'
    b'5!fTJM_`Y@9)Udqdj$3f>=D=_ut#8zz#f4;0(%7Z2<#EqBd|wckH8**Jpy|K_NW>5s3*l9y}fb+7w{3_Bfv+1j{qM5J_39M_z3V3'
    b';3L3CfR6wl0X_nJ1o#N>5#Xb^$T~bOf{p|o2|5yVB<M)ck)R_%M}m$79SJ%TbR_6V(2<}cK}Uj)1RV)F5_BZ!NYIg>BSA-kjszVE'
    b'I?~<Jkz&q?EE@+)36>HpC0I(Zlwc{rQi7!fO9_?|EG1Y<u#{ja!BT>y1WO5)5-cTHO0bk*DZx^Lr36a}mJ%$bW-O(?l%+Hffq+<d'
    b'&lm3OlMhJ=k`g2(NJ@~DASppof}{jV36c^dB}ht;lprZVQi7xeNePmYTUTfnpD0bwqa!6?OTd<ZEdg5swghYm*b=ZMU`xQ3fGq)A'
    b'0=5Kf3D^>_C16XymVhk*TLQKOYzf#Buq9wiz?Oh5eRJ4S{Jp0nbqARVG81Gb$V`x#ATvQ`g3JV&2{IF8Cdf>XnIJPkW`fKFnF%rz'
    b'WG2W=keMJeL1u!?1epmk6J#bCnd#&I0V(AEr2'
)))


@dataclass(frozen=True)
class ReviewedReleaseTransition:
    before: str
    target: str
    migrations: tuple
    migration_blobs: tuple
    nullable_columns: tuple
    schema_prefix_contracts: tuple
    expected_new_values: tuple


class MaintenanceError(Exception):
    pass


def require(condition, message):
    if not condition:
        raise MaintenanceError(message)


def migration_ids(git, sha):
    # EF has older migrations without a Designer file. Read their attributes as well.
    output = git("grep", "-h", "-E", r'^[[:space:]]*\[Migration\("[^"]+"\)\]', sha, "--", MIGRATIONS)
    ids = re.findall(r'^\s*\[Migration\("([0-9]{14}_[A-Za-z][A-Za-z0-9_]*)"\)\]\s*$', output, re.M)
    require(len(ids) == len(output.splitlines()) and len(ids) == len(set(ids)) and ids,
            "Invalid or duplicate EF migration attributes in Git")
    return tuple(sorted(ids))


def classify(git, before, target, ordinary=None):
    git("merge-base", "--is-ancestor", before, target)
    diff = git("diff", "--raw", "--no-abbrev", "--no-renames", "--no-ext-diff", "-z",
               before, target, "--").split("\0")
    paths = []
    pairs = {}
    for header, path in zip(diff[:-1:2], diff[1::2], strict=True):
        old, new, _, _, status = header.removeprefix(":").split()
        if path in RELATED:
            require((status, old, new) == ("M", "100644", "100644"), "Invalid related file change")
        else:
            name = path.removeprefix(MIGRATIONS + "/")
            match = PAIR.fullmatch(name) if name != path else None
            if match:
                require((status, old, new) == ("A", "000000", "100644"),
                        f"Migration rehearsal refuses path {ascii(path)}")
                pairs.setdefault(match[1], set()).add(bool(match[2]))
            else:
                require(name == path and ordinary and ordinary(path)
                        and (status, old, new) in {("A", "000000", "100644"), ("M", "100644", "100644"),
                                                   ("D", "100644", "000000")},
                        f"Migration rehearsal refuses path {ascii(path)}")
        paths.append(path)
    before_ids, target_ids = migration_ids(git, before), migration_ids(git, target)
    require(pairs and all(parts == {False, True} for parts in pairs.values())
            and target_ids == before_ids + tuple(sorted(pairs))
            and all(migration > before_ids[-1] for migration in pairs),
            "Expected complete, append-only EF migration pairs")
    return paths, before_ids, target_ids


def review_plan(raw, before, target):
    def unique_keys(pairs):
        result = dict(pairs)
        require(len(pairs) == len(result), "Duplicate maintenance review key")
        return result

    plan = json.loads(raw, object_pairs_hook=unique_keys)
    require(isinstance(plan, dict) and set(plan) == {"before", "target", "add_nullable_columns"}
            and plan["before"] == before and plan["target"] == target,
            "Maintenance review does not match the running and target SHAs")
    columns = plan["add_nullable_columns"]
    require(isinstance(columns, list) and 0 < len(columns) <= 32, "Review must list the exact nullable columns")
    reviewed = []
    keys = set()
    for entry in columns:
        require(isinstance(entry, list) and len(entry) == 3 and all(isinstance(value, str) for value in entry),
                "Invalid maintenance review column")
        table, column, kind = entry
        key = table.lower(), column.lower()
        require(IDENTIFIER.fullmatch(table) and IDENTIFIER.fullmatch(column)
                and table.lower() not in (HISTORY.lower(), LOCK.lower()) and not table.lower().startswith("sqlite_")
                and kind in ("TEXT", "INTEGER") and key not in keys, "Invalid or duplicate reviewed column")
        keys.add(key)
        reviewed.append(tuple(entry))
    return tuple(reviewed)


def bootstrap_paths(git, before, target, raw):
    if not raw:
        return set()
    record = json.loads(raw, object_pairs_hook=lambda pairs: _unique_json_pairs(pairs))
    if record.get("target") != target:
        return set()
    require(set(record) in ({"before", "target", "blobs"}, {"before", "target", "blobs", "reviewed_build_inputs"}) and record["before"] == before
            and isinstance(record["blobs"], dict), "Tool installation review does not match this transition")
    blobs = record["blobs"]
    require(set(blobs) <= BOOTSTRAP_PATHS, "Tool installation review contains an unsupported path")
    for path, blob in blobs.items():
        require(re.fullmatch(r"[0-9a-f]{40}", blob) and git("rev-parse", f"{target}:{path}").strip() == blob,
                "Reviewed installation blob differs from the candidate")
    reviewed_paths = set(blobs)
    build_inputs = record.get("reviewed_build_inputs", {})
    require(isinstance(build_inputs, dict) and set(build_inputs) <= REVIEWED_BUILD_INPUTS,
            "Tooling review contains an unsupported build input")
    if build_inputs:
        diff = git("diff", "--raw", "--no-abbrev", "--no-renames", "--no-ext-diff", "-z", before, target, "--").split("\0")
        actual = {}
        for header, path in zip(diff[:-1:2], diff[1::2], strict=True):
            old_mode, new_mode, old_blob, new_blob, status = header.removeprefix(":").split()
            actual[path] = (status, old_mode, new_mode, old_blob, new_blob)
        for path, review in build_inputs.items():
            require(isinstance(review, dict) and set(review) == {"status", "before", "after"} and path in actual,
                    "Invalid reviewed build input")
            status, old_mode, new_mode, old_blob, new_blob = actual[path]
            require(status in {"A", "M"} and new_mode == "100644"
                    and (status, review["before"], review["after"]) ==
                    (status, None if status == "A" else old_blob, new_blob)
                    and (status == "A" and old_mode == "000000" or status == "M" and old_mode == "100644"),
                    "Reviewed build input differs from the complete Git diff")
            reviewed_paths.add(path)
        for path, (status, old_mode, new_mode, _, _) in actual.items():
            critical = path in BOOTSTRAP_PATHS or path.lower().startswith((".github/workflows/", "dataaccesslayer/administration/")) or path.lower().endswith((".csproj", ".props", ".targets", ".sln", ".sh"))
            if critical and path not in reviewed_paths:
                require(path in BOOTSTRAP_PATHS and status == "M" and old_mode == new_mode == "100644",
                        f"Unreviewed critical build input {ascii(path)}")
    return reviewed_paths


def _unique_json_pairs(pairs):
    result = dict(pairs)
    require(len(result) == len(pairs), "Duplicate maintenance review key")
    return result


ADD_COLUMN = re.compile(
    r'migrationBuilder\.AddColumn<(string|long)>\s*\(\s*'
    r'name:\s*"([A-Za-z_][A-Za-z0-9_]*)",\s*table:\s*"([A-Za-z_][A-Za-z0-9_]*)",\s*'
    r'type:\s*"(TEXT|INTEGER)",\s*(?:maxLength:\s*[1-9][0-9]*,\s*)?nullable:\s*true\s*\);'
)


def validate_reviewed_schema_contracts(contracts):
    require(len(contracts) == 8 and tuple(item.get("migrationId") for item in contracts) ==
            ("20261115135735_RefreshManualShiftOffer",) + REVIEWED_RELEASE_MIGRATIONS,
            "Reviewed schema contracts do not cover the exact PROD2 baseline and seven migrations")
    expected_object_changes = (
        ({("index", "ux_shift_swap_open_return_slot"), ("index", "ux_shift_swap_open_slot")},
         {("index", "ix_shift_swap_open_return_slot"), ("index", "ix_shift_swap_open_slot")}, set()),
        (set(), set(), {("table", "employee_account")}),
        (set(), set(), {("table", "workflow_log_entry")}),
        (set(), set(), {("table", "shift_correction_request")}),
        (set(), set(), {("table", "communication_message")}),
        (set(), set(), {("table", "communication_message")}),
        (set(), set(), {("table", "communication_message")}),
    )
    added_fields = (
        {"employee_account": {"last_browser_launch_at_utc", "last_pwa_launch_at_utc"}},
        {"workflow_log_entry": {"launch_mode"}},
        {"shift_correction_request": {"operation", "removal_snapshot_json"}},
        {"communication_message": {"gif_attachment_json"}},
        {"communication_message": {"photo_attachment_json"}},
        {"communication_message": {"content_blocks_json"}},
    )
    for index, (before, after) in enumerate(zip(contracts, contracts[1:])):
        old = {(row[0], row[1]): row for row in before["schemaObjects"]}
        new = {(row[0], row[1]): row for row in after["schemaObjects"]}
        deleted, inserted, modified = expected_object_changes[index]
        require(old.keys() - new.keys() == deleted and new.keys() - old.keys() == inserted
                and {key for key in old.keys() & new.keys() if old[key] != new[key]} == modified,
                f"Reviewed schema prefix {index + 1} contains an unexpected schema-object change")
        if index:
            before_tables = {item["table"]: {row[1] for row in item["columns"]} for item in before["tables"]}
            after_tables = {item["table"]: {row[1] for row in item["columns"]} for item in after["tables"]}
            for table, names in added_fields[index - 1].items():
                require(after_tables[table] - before_tables[table] == names,
                        f"Reviewed migration added unexpected columns to {table}")
            require(all(after_tables[table] == before_tables[table]
                        for table in after_tables if table not in added_fields[index - 1]),
                    "Reviewed migration changed an unapproved affected table's columns")
    swap = {row[1]: row[3] for row in contracts[1]["schemaObjects"] if row[0] == "index"}
    require(swap.get("ix_shift_swap_open_return_slot") ==
            'CREATE INDEX "ix_shift_swap_open_return_slot" ON "shift_swap_request" '
            '("return_schedule_slot_id", "status") WHERE status = \'Open\' AND request_kind = \'Exchange\' AND return_schedule_slot_id IS NOT NULL'
            and swap.get("ix_shift_swap_open_slot") ==
            'CREATE INDEX "ix_shift_swap_open_slot" ON "shift_swap_request" '
            '("schedule_slot_id", "status") WHERE status = \'Open\'',
            "Reviewed swap indexes differ from the approved non-unique filtered definitions")
    correction = next(row[3] for row in contracts[4]["schemaObjects"] if row[1] == "shift_correction_request")
    require('"operation" TEXT NOT NULL DEFAULT \'ChangeTimes\'' in correction
            and '"removal_snapshot_json" TEXT NULL' in correction
            and "operation = 'RemoveShift' AND removal_snapshot_json IS NOT NULL" in correction
            and "operation = 'ChangeTimes' AND removal_snapshot_json IS NULL" in correction,
            "Reviewed correction-table contract omits the approved legacy/new request shapes")
    return True


def migration_plan(git, before, target, pending):
    pending = tuple(pending)
    if set(pending) & set(REVIEWED_RELEASE_MIGRATIONS):
        require(before == RELEASE_BASELINE and pending == REVIEWED_RELEASE_MIGRATIONS,
                "Reviewed seven-migration transition requires the exact pinned PROD2 baseline and complete ordered chain")
        validate_reviewed_schema_contracts(REVIEWED_SCHEMA_PREFIX_CONTRACTS)
        blobs = []
        for migration in REVIEWED_RELEASE_MIGRATIONS:
            expected = REVIEWED_RELEASE_BLOBS[migration]
            actual = (git("rev-parse", f"{target}:{MIGRATIONS}/{migration}.cs").strip(),
                      git("rev-parse", f"{target}:{MIGRATIONS}/{migration}.Designer.cs").strip())
            require(actual == expected, f"Reviewed migration source or Designer blob changed: {migration}")
            blobs.append((migration, *actual))
        nullable = (
            ("employee_account", "last_browser_launch_at_utc", "TEXT"),
            ("employee_account", "last_pwa_launch_at_utc", "TEXT"),
            ("workflow_log_entry", "launch_mode", "TEXT"),
            ("communication_message", "gif_attachment_json", "TEXT"),
            ("communication_message", "photo_attachment_json", "TEXT"),
            ("communication_message", "content_blocks_json", "TEXT"),
        )
        expected_new_values = (
            (2, "employee_account", "last_browser_launch_at_utc", None),
            (2, "employee_account", "last_pwa_launch_at_utc", None),
            (3, "workflow_log_entry", "launch_mode", None),
            (4, "shift_correction_request", "operation", "ChangeTimes"),
            (4, "shift_correction_request", "removal_snapshot_json", None),
            (5, "communication_message", "gif_attachment_json", None),
            (6, "communication_message", "photo_attachment_json", None),
            (7, "communication_message", "content_blocks_json", None),
        )
        return ReviewedReleaseTransition(before, target, pending, tuple(blobs), nullable,
                                         tuple(REVIEWED_SCHEMA_PREFIX_CONTRACTS), expected_new_values)
    columns = []
    for migration in pending:
        source = git("show", f"{target}:{MIGRATIONS}/{migration}.cs")
        up = re.search(r"protected override void Up\(MigrationBuilder migrationBuilder\)\s*\{([^{}]*)\}", source)
        require(up is not None, "Maintenance supports generated nullable column migrations")
        operations = ADD_COLUMN.findall(up[1])
        require(operations and not ADD_COLUMN.sub("", up[1]).strip(),
                "Maintenance migration contains an unsupported operation")
        for clr, column, table, affinity in operations:
            require((clr, affinity) in {("string", "TEXT"), ("long", "INTEGER")},
                    "Unexpected SQLite affinity in maintenance migration")
            columns.append((table, column, affinity))
    return review_plan(json.dumps({"before": before, "target": target, "add_nullable_columns": columns}),
                       before, target)


def open_db(path, mode, timeout=5):
    info = path.lstat()
    require(stat.S_ISREG(info.st_mode) and info.st_size > 0 and info.st_nlink == 1,
            "Expected an existing regular SQLite database")
    return sqlite3.connect(path.as_uri() + f"?mode={mode}", uri=True, timeout=timeout)


def verify(db, expected_ids):
    deadline = time.monotonic() + 120
    db.set_progress_handler(lambda: int(time.monotonic() >= deadline), 10000)
    try:
        require(db.execute("PRAGMA integrity_check").fetchall() == [("ok",)], "SQLite integrity check failed")
    finally:
        db.set_progress_handler(None, 0)
    history = tuple(db.execute(f'SELECT MigrationId, ProductVersion FROM "{HISTORY}" ORDER BY MigrationId'))
    require(tuple(row[0] for row in history) == expected_ids and all(row[1] for row in history),
            "SQLite migration history differs from the approved revision")
    if db.execute("SELECT 1 FROM sqlite_schema WHERE type='table' AND name=?", (LOCK,)).fetchone():
        require(not db.execute(f'SELECT 1 FROM "{LOCK}" LIMIT 1').fetchone(), "SQLite EF migration lock is held")
    return history


def snapshot(source, destination, expected_ids, *, quiesced=False):
    # The destination is private and new. Successful callers may retain it as a recovery artifact.
    descriptor = os.open(destination, os.O_CREAT | os.O_EXCL | os.O_WRONLY, 0o600)
    os.close(descriptor)
    try:
        deadline = time.monotonic() + 120

        def progress(*_):
            require(time.monotonic() < deadline, "SQLite snapshot timed out")

        with closing(open_db(source, "rw" if quiesced else "ro", timeout=0 if quiesced else 5)) as reader:
            if quiesced:
                # Hold a reserved writer lock through the copy using a separate read connection.
                reader.execute("BEGIN IMMEDIATE")
                with closing(open_db(source, "ro")) as copy_reader, closing(sqlite3.connect(destination)) as copy:
                    copy_reader.backup(copy, pages=256, progress=progress)
                    verify(copy, expected_ids)
            else:
                with closing(sqlite3.connect(destination)) as copy:
                    reader.backup(copy, pages=256, progress=progress)
                    verify(copy, expected_ids)
        with destination.open("r+b") as backup:
            os.fsync(backup.fileno())
        if os.name != "nt":
            descriptor = os.open(destination.parent, os.O_RDONLY | os.O_DIRECTORY)
            try:
                os.fsync(descriptor)
            finally:
                os.close(descriptor)
    except BaseException:
        destination.unlink(missing_ok=True)
        raise


def digest_file(path):
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


MEDIA_DIRECTORY = "communication-gifs"
BACKUP_MANIFEST = "backup-manifest.json"
MEDIA_LIMIT = 3 * 1024 * 1024


def _json_object(pairs):
    result = {}
    seen = set()
    for key, value in pairs:
        folded = key.casefold()
        require(folded not in seen, "Duplicate JSON field")
        seen.add(folded)
        result[key] = value
    return result


def _attachment_key(value, references):
    require(isinstance(value, dict), "Malformed communication attachment")
    fields = {key.casefold(): item for key, item in value.items()}
    key, size = fields.get("key"), fields.get("size")
    require(isinstance(key, str) and re.fullmatch(r"[0-9a-fA-F]{8}-?[0-9a-fA-F]{4}-?[0-9a-fA-F]{4}-?[0-9a-fA-F]{4}-?[0-9a-fA-F]{12}", key)
            and isinstance(size, int) and not isinstance(size, bool) and 0 < size <= MEDIA_LIMIT,
            "Invalid communication attachment key or size")
    compact = key.replace("-", "").lower()
    name = compact + ".gif"
    require(name not in references or references[name] == size, "Contradictory communication attachment sizes")
    references[name] = size


def referenced_media(database):
    columns = {row[1].casefold() for row in database.execute('PRAGMA table_info("communication_message")')}
    known = [name for name in ("gif_attachment_json", "photo_attachment_json", "content_blocks_json") if name in columns]
    if not known:
        return {}
    references = {}
    for row in database.execute("SELECT " + ",".join('"' + name + '"' for name in known) + ' FROM "communication_message"'):
        for name, raw in zip(known, row, strict=True):
            if raw is None:
                continue
            require(isinstance(raw, str), "Communication attachment JSON must be text")
            parsed = json.loads(raw, object_pairs_hook=_json_object)
            if name == "content_blocks_json":
                require(isinstance(parsed, list), "Malformed communication content blocks")
                for block in parsed:
                    require(isinstance(block, dict), "Malformed communication content block")
                    fields = {key.casefold(): item for key, item in block.items()}
                    if fields.get("type") in ("photo", "gif"):
                        _attachment_key(fields.get("media"), references)
            else:
                _attachment_key(parsed, references)
    return references


def media_stamp(source_dir):
    source_dir = Path(source_dir)
    if not source_dir.exists():
        return {}
    info = source_dir.lstat()
    require(stat.S_ISDIR(info.st_mode) and not stat.S_ISLNK(info.st_mode), "Unsafe communication media directory")
    result = {}
    for path in sorted(source_dir.iterdir()):
        item = path.lstat()
        require(stat.S_ISREG(item.st_mode) and not stat.S_ISLNK(item.st_mode) and re.fullmatch(r"[0-9a-f]{32}\.gif", path.name),
                "Unexpected communication media entry")
        result[path.name] = [item.st_size, digest_file(path), item.st_uid if hasattr(item, "st_uid") else None,
                             item.st_gid if hasattr(item, "st_gid") else None, stat.S_IMODE(item.st_mode)]
    return result


def copy_media_snapshot(snapshot_path, source_dir, dest_dir, *, full=False):
    source_dir, dest_dir = Path(source_dir), Path(dest_dir)
    with closing(open_db(Path(snapshot_path), "ro")) as database:
        references = referenced_media(database)
    source_stamp = media_stamp(source_dir)
    names = set(source_stamp) if full else set(references)
    require(set(references) <= set(source_stamp), "Referenced communication media is missing")
    require(not names or source_dir.is_dir(), "Communication media directory is missing")
    dest_dir.mkdir(mode=0o700)
    entries = []
    for name in sorted(names):
        size, expected_hash, uid, gid, mode = source_stamp[name]
        require(size > 0 and size <= MEDIA_LIMIT, "Invalid communication media size")
        if name in references:
            require(references[name] == size, "Communication media size does not match its database reference")
        source, destination = source_dir / name, dest_dir / name
        shutil.copyfile(source, destination, follow_symlinks=False)
        require(digest_file(source) == expected_hash and digest_file(destination) == expected_hash
                and destination.stat().st_size == size, "Communication media changed during snapshot")
        if os.name != "nt":
            os.chown(destination, uid, gid)
            os.chmod(destination, mode)
        entries.append({"name": name, "size": size, "sha256": expected_hash,
                        "permissions": {"uid": uid, "gid": gid, "mode": mode}})
    require(media_stamp(source_dir) == source_stamp, "Communication media changed during snapshot")
    if os.name != "nt":
        if source_dir.exists():
            directory_info = source_dir.stat()
            os.chown(dest_dir, directory_info.st_uid, directory_info.st_gid)
            os.chmod(dest_dir, stat.S_IMODE(directory_info.st_mode))
        else:
            os.chmod(dest_dir, 0o700)
        descriptor = os.open(dest_dir, os.O_RDONLY | os.O_DIRECTORY)
        try:
            os.fsync(descriptor)
        finally:
            os.close(descriptor)
    return entries


def _write_backup_manifest(bundle, database_path, media, mode):
    database_path = Path(database_path)
    info = database_path.stat()
    media_directory = Path(bundle) / MEDIA_DIRECTORY
    media_info = media_directory.stat() if media_directory.exists() else None
    manifest = {"version": 1, "database": {"name": database_path.name, "size": info.st_size,
                 "sha256": digest_file(database_path), "permissions": {"uid": info.st_uid if hasattr(info, "st_uid") else None,
                 "gid": info.st_gid if hasattr(info, "st_gid") else None, "mode": stat.S_IMODE(info.st_mode)}},
                "media": sorted(media, key=lambda item: item["name"]), "permissions": {
                    "mediaDirectory": {"uid": media_info.st_uid if media_info and hasattr(media_info, "st_uid") else None,
                                       "gid": media_info.st_gid if media_info and hasattr(media_info, "st_gid") else None,
                                       "mode": stat.S_IMODE(media_info.st_mode) if media_info else 0o700}}, "mode": mode,
                "createdAtUtc": time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime())}
    temporary = Path(bundle) / (BACKUP_MANIFEST + ".tmp")
    with temporary.open("x", encoding="utf-8") as stream:
        json.dump(manifest, stream, ensure_ascii=False, separators=(",", ":"))
        stream.flush()
        os.fsync(stream.fileno())
    os.replace(temporary, Path(bundle) / BACKUP_MANIFEST)
    if os.name != "nt":
        (Path(bundle) / BACKUP_MANIFEST).chmod(0o600)
    sync_dir(Path(bundle))
    return manifest


def verify_backup_bundle(bundle, manifest=None):
    bundle = Path(bundle)
    bundle_info = bundle.lstat()
    require(stat.S_ISDIR(bundle_info.st_mode) and not stat.S_ISLNK(bundle_info.st_mode), "Unsafe backup bundle path")
    manifest_path = bundle / BACKUP_MANIFEST
    manifest_info = manifest_path.lstat()
    require(stat.S_ISREG(manifest_info.st_mode) and manifest_info.st_nlink == 1, "Unsafe backup manifest file")
    if os.name != "nt":
        require(stat.S_IMODE(manifest_info.st_mode) == 0o600, "Backup manifest permissions are unsafe")
    if manifest is None:
        with manifest_path.open(encoding="utf-8") as stream:
            manifest = json.load(stream, object_pairs_hook=_json_object)
    require(isinstance(manifest, dict) and manifest.get("version") == 1 and manifest.get("mode") in ("referenced", "fenced-full"),
            "Unknown backup manifest version or mode")
    require(isinstance(manifest.get("createdAtUtc"), str)
            and re.fullmatch(r"[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}(?:\.[0-9]+)?Z", manifest["createdAtUtc"]),
            "Invalid backup creation timestamp")
    database = manifest.get("database")
    require(isinstance(database, dict) and set(database) >= {"name", "size", "sha256"}
            and database["name"] in ("SQLite.db", "final.db") and isinstance(database["size"], int)
            and database["size"] >= 0 and re.fullmatch(r"[0-9a-f]{64}", str(database["sha256"])), "Invalid backup database entry")
    db_path = bundle / database["name"]
    info = db_path.lstat()
    require(stat.S_ISREG(info.st_mode) and info.st_nlink == 1 and info.st_size == database["size"]
            and digest_file(db_path) == database["sha256"], "Backup database checksum or type is invalid")
    database_permissions = database.get("permissions", {})
    if os.name != "nt" and database_permissions:
        require(info.st_uid == database_permissions.get("uid") and info.st_gid == database_permissions.get("gid")
                and stat.S_IMODE(info.st_mode) == database_permissions.get("mode"), "Backup database permissions differ from manifest")
    media = manifest.get("media")
    require(isinstance(media, list), "Invalid backup media list")
    expected = {}
    for item in media:
        require(isinstance(item, dict) and set(item) >= {"name", "size", "sha256"}
                and re.fullmatch(r"[0-9a-f]{32}\.gif", str(item["name"])) and isinstance(item["size"], int)
                and 0 <= item["size"] <= MEDIA_LIMIT and re.fullmatch(r"[0-9a-f]{64}", str(item["sha256"]))
                and item["name"] not in expected, "Invalid or duplicate backup media entry")
        expected[item["name"]] = item
    require([item["name"] for item in media] == sorted(expected), "Backup media manifest is not sorted")
    media_dir = bundle / MEDIA_DIRECTORY
    info = media_dir.lstat()
    require(stat.S_ISDIR(info.st_mode) and not stat.S_ISLNK(info.st_mode), "Unsafe or missing backup media directory")
    directory_permissions = manifest.get("permissions", {}).get("mediaDirectory", {})
    if os.name != "nt" and directory_permissions:
        require(info.st_uid == directory_permissions.get("uid") and info.st_gid == directory_permissions.get("gid")
                and stat.S_IMODE(info.st_mode) == directory_permissions.get("mode"), "Backup media directory permissions differ from manifest")
    actual = {path.name for path in media_dir.iterdir()}
    require(actual == set(expected), "Backup media membership differs from the manifest")
    for name, item in expected.items():
        path = media_dir / name
        info = path.lstat()
        require(stat.S_ISREG(info.st_mode) and info.st_nlink == 1 and info.st_size == item["size"]
                and digest_file(path) == item["sha256"], "Backup media checksum or type is invalid")
        permissions = item.get("permissions", {})
        if os.name != "nt" and permissions:
            require(info.st_uid == permissions.get("uid") and info.st_gid == permissions.get("gid")
                    and stat.S_IMODE(info.st_mode) == permissions.get("mode"), "Backup media permissions differ from manifest")
    with closing(open_db(db_path, "ro")) as connection:
        references = referenced_media(connection)
        require(set(references) <= set(expected) and all(expected[name]["size"] == size for name, size in references.items()),
                "Backup references missing or mismatched media")
        if manifest["mode"] == "referenced":
            require(set(references) == set(expected), "Referenced backup contains unexpected media")
    return db_path


def sqlite_files(path):
    return (path, *(path.with_name(path.name + suffix) for suffix in ("-wal", "-shm", "-journal")))


def database_stamp(path):
    result = {}
    for entry in sqlite_files(path):
        try:
            info = entry.lstat()
        except FileNotFoundError:
            require(entry != path, "SQLite database is missing")
            continue
        require(stat.S_ISREG(info.st_mode) and info.st_nlink == 1, "Unexpected SQLite file type")
        if entry != path and (entry.name.endswith("-shm") or info.st_size == 0):
            # Readers of a checkpointed WAL database can create empty journal files.
            # Their presence and the shared-memory index do not represent data changes.
            continue
        result[entry.name] = [info.st_dev, info.st_ino, info.st_size, digest_file(entry)]
    return result


def checkpoint_for_read_only_health(path):
    # The health process uses an immutable SQLite URI on a read-only mount. It must see
    # a checkpointed main file, including the final committed WAL pages.
    with closing(open_db(path, "rw", timeout=0)) as database:
        if database.execute("PRAGMA journal_mode").fetchone() == ("wal",):
            require(database.execute("PRAGMA wal_checkpoint(TRUNCATE)").fetchone() == (0, 0, 0),
                    "SQLite WAL could not be fully checkpointed for health verification")
    wal = sqlite_files(path)[1]
    require(not wal.exists() or wal.stat().st_size == 0, "SQLite contains uncheckpointed WAL changes")


def final_backup(database, bundle, history, assert_quiet, record):
    assert_quiet()
    backup = bundle / "final.db"
    snapshot(database, backup, history, quiesced=True)
    assert_quiet()
    media = copy_media_snapshot(backup, database.parent / MEDIA_DIRECTORY, bundle / MEDIA_DIRECTORY, full=True)
    manifest = _write_backup_manifest(bundle, backup, media, "fenced-full")
    manifest_sha256 = digest_file(bundle / BACKUP_MANIFEST)
    metadata = {"sha256": digest_file(backup), "source": database_stamp(database), "history": list(history)}
    metadata["media_manifest_sha256"] = manifest_sha256
    metadata["media_stamp"] = media_stamp(database.parent / MEDIA_DIRECTORY)
    # Prove the checkpoint includes every committed row at the same recovery point.
    verify_migration(backup, database, history, history, ())
    assert_quiet()
    require(database_stamp(database) == metadata["source"] and media_stamp(database.parent / MEDIA_DIRECTORY) == metadata["media_stamp"],
            "Database or communication media changed during final backup")
    require(verify_backup_bundle(bundle, manifest) == backup, "Final backup bundle verification failed")
    record(metadata)
    return metadata


def verify_final_backup(bundle, metadata):
    backup = bundle / "final.db"
    info = backup.lstat()
    require(stat.S_ISREG(info.st_mode) and info.st_nlink == 1
            and (os.name == "nt" or (info.st_uid == os.geteuid() and stat.S_IMODE(info.st_mode) == 0o600)),
            "Final backup permissions are unsafe")
    require(digest_file(backup) == metadata["sha256"], "Final backup checksum mismatch")
    with closing(open_db(backup, "ro")) as database:
        verify(database, tuple(metadata["history"]))
        references = referenced_media(database)
    manifest_path = bundle / BACKUP_MANIFEST
    if manifest_path.exists():
        require(digest_file(manifest_path) == metadata.get("media_manifest_sha256"), "Final media manifest checksum mismatch")
        with manifest_path.open(encoding="utf-8") as stream:
            manifest = json.load(stream, object_pairs_hook=_json_object)
        require(manifest.get("mode") == "fenced-full", "Final recovery backup must contain a fenced full media snapshot")
        require(verify_backup_bundle(bundle) == backup, "Final backup bundle verification failed")
    else:
        require(not references and not (bundle / MEDIA_DIRECTORY).exists(), "Historical DB-only backup has communication media references")
    return backup


def restore_database(database, bundle, metadata, assert_quiet, consume_restore, expected_stamp=None, record_progress=None):
    """Replace a fenced database once and preserve the failed database and its journals."""
    backup = verify_final_backup(bundle, metadata)
    assert_quiet()
    files = database_stamp(database)
    require(expected_stamp is None or expected_stamp == files, "Database changed after recovery verification")
    saved_main = metadata["source"][database.name]
    require(files[database.name][:2] == saved_main[:2], "Recovery database was replaced outside maintenance")
    failed = bundle / "failed-database"
    failed.mkdir(mode=0o700)
    descriptor, pending = tempfile.mkstemp(dir=database.parent, prefix=".gf3-restore-")
    os.close(descriptor)
    replacement = Path(pending)
    live_media = database.parent / MEDIA_DIRECTORY
    staged_media = Path(tempfile.mkdtemp(prefix=".gf3-restore-media-", dir=database.parent))
    staged_media.rmdir()
    old_media = database.parent / (".gf3-old-media-" + secrets.token_hex(12))
    has_media_snapshot = (bundle / BACKUP_MANIFEST).exists()
    restore_consumed = False

    def progress(stage):
        if record_progress:
            record_progress({"stage": stage, "staged_database": str(replacement),
                             "staged_media": str(staged_media) if has_media_snapshot else None,
                             "failed_bundle": str(failed), "old_media": str(old_media) if has_media_snapshot else None})

    try:
        info = database.lstat()
        shutil.copyfile(backup, replacement)
        require(digest_file(replacement) == metadata["sha256"], "Recovery copy checksum mismatch")
        if os.name != "nt":
            os.chown(replacement, info.st_uid, info.st_gid)
        replacement.chmod(stat.S_IMODE(info.st_mode))
        with replacement.open("r+b") as stream:
            os.fsync(stream.fileno())
        if has_media_snapshot:
            with (bundle / BACKUP_MANIFEST).open(encoding="utf-8") as stream:
                manifest = json.load(stream, object_pairs_hook=_json_object)
            shutil.copytree(bundle / MEDIA_DIRECTORY, staged_media, copy_function=shutil.copyfile)
            for item in manifest["media"]:
                target = staged_media / item["name"]
                if os.name != "nt" and item.get("permissions"):
                    permissions = item["permissions"]
                    if permissions.get("uid") is not None and permissions.get("gid") is not None:
                        os.chown(target, permissions["uid"], permissions["gid"])
                    target.chmod(permissions.get("mode", 0o600))
            directory_permissions = manifest.get("permissions", {}).get("mediaDirectory", {})
            if os.name != "nt" and directory_permissions:
                if directory_permissions.get("uid") is not None and directory_permissions.get("gid") is not None:
                    os.chown(staged_media, directory_permissions["uid"], directory_permissions["gid"])
                staged_media.chmod(directory_permissions.get("mode", 0o700))
            require(media_stamp(staged_media) == {item["name"]: [item["size"], item["sha256"],
                    item.get("permissions", {}).get("uid"), item.get("permissions", {}).get("gid"),
                    item.get("permissions", {}).get("mode", 0o600)] for item in manifest["media"]},
                    "Staged recovery media differs from the verified snapshot")
        progress("staged")
        assert_quiet()
        require(database_stamp(database) == files, "Database changed immediately before recovery")
        consume_restore()
        restore_consumed = True
        # Keep a byte-for-byte forensic copy, including any uncheckpointed data. Copying
        # across filesystems is safe here because the writer fence has been verified.
        for entry in sqlite_files(database):
            if entry.exists():
                shutil.copyfile(entry, failed / entry.name)
                (failed / entry.name).chmod(0o600)
                with (failed / entry.name).open("r+b") as stream:
                    os.fsync(stream.fileno())
        sync_dir(failed)
        if has_media_snapshot and live_media.exists():
            shutil.copytree(live_media, failed / MEDIA_DIRECTORY, copy_function=shutil.copy2)
        sync_dir(bundle)
        progress("forensic-copy-complete")
        assert_quiet()
        require(database_stamp(database) == files, "Database changed while preserving recovery evidence")
        for entry in sqlite_files(database)[1:]:
            entry.unlink(missing_ok=True)
        os.replace(replacement, database)
        sync_dir(database.parent)
        progress("database-installed")
        if has_media_snapshot:
            if live_media.exists():
                os.replace(live_media, old_media)
            os.replace(staged_media, live_media)
            sync_dir(database.parent)
            require(media_stamp(live_media) == media_stamp(bundle / MEDIA_DIRECTORY), "Restored media checksum mismatch")
            progress("media-installed")
        with closing(open_db(database, "ro")) as restored:
            verify(restored, tuple(metadata["history"]))
        verify_migration(backup, database, tuple(metadata["history"]), tuple(metadata["history"]), ())
        progress("verified")
    finally:
        replacement.unlink(missing_ok=True)
        if staged_media.exists() and not restore_consumed:
            shutil.rmtree(staged_media)


def sync_dir(directory):
    if os.name != "nt":
        descriptor = os.open(directory, os.O_RDONLY | os.O_DIRECTORY)
        try:
            os.fsync(descriptor)
        finally:
            os.close(descriptor)


TRANSITIONS = {
    "REHEARSED": {"FENCING"},
    "FENCING": {"FENCED"},
    "FENCED": {"BACKED_UP", "RECOVERY_READY"},
    "BACKED_UP": {"MIGRATING", "RECOVERY_READY"},
    "MIGRATING": {"VERIFYING", "RESTORING", "RECOVERY_READY"},
    "VERIFYING": {"RESTORING", "ACTIVATING_GIT"},
    "ACTIVATING_GIT": {"READY_TO_REOPEN", "RESTORING"},
    "READY_TO_REOPEN": {"WRITES_MAY_RESUME"},
    "RESTORING": {"RECOVERY_READY"},
    "RECOVERY_READY": {"WRITES_MAY_RESUME"},
    "WRITES_MAY_RESUME": {"COMPLETED", "RECOVERED"},
}


class Journal:
    """Root-owned durable journal. The active record blocks every managed mutator."""

    def __init__(self, state, bundle, store):
        self.bundle = bundle
        self.path = state / "maintenance-active.json"
        self.store = store

    def begin(self, identity):
        require(not os.path.lexists(self.path), "An unfinished maintenance transaction already exists")
        self.write({**identity, "bundle": self.bundle.name, "phase": "REHEARSED", "writes_may_resume": False})

    def read(self):
        state = json.loads(self.path.read_bytes())
        require(state["bundle"] == self.bundle.name, "Maintenance journal belongs to a different transaction")
        return state

    def write(self, state):
        self.store(self.path, state)
        sync_dir(self.path.parent)

    def advance(self, phase, **fields):
        state = self.read()
        prior = state["phase"]
        require(phase in TRANSITIONS.get(prior, ()), f"Invalid maintenance transition {prior} -> {phase}")
        require(phase != "RESTORING" or state["writes_may_resume"] is False,
                "Automatic database recovery is unsafe after writers may resume")
        state.update(fields, phase=phase,
                     writes_may_resume=state["writes_may_resume"] or phase == "WRITES_MAY_RESUME")
        self.write(state)

    def require_phase(self, phase):
        require(self.read()["phase"] == phase, f"Expected maintenance phase {phase}")

    def fail(self, stage):
        state = self.read()
        self.write({**state, "phase": "RECOVERY_REQUIRED", "failed_in": state["phase"], "failure": stage})

    def finish(self):
        state = self.read()
        require(state["phase"] in {"COMPLETED", "RECOVERED"}, "Do not release an incomplete maintenance transaction")
        self.store(self.bundle / "transaction.json", state)
        sync_dir(self.bundle)
        self.path.unlink()
        sync_dir(self.path.parent)


def quoted(value):
    return '"' + value.replace('"', '""') + '"'


def schema(db):
    return {(kind, name): sql for kind, name, sql in
            db.execute("SELECT type, name, sql FROM sqlite_schema WHERE sql IS NOT NULL")}


REVIEWED_SCHEMA_TABLES = (
    "communication_message", "employee_account", "shift_correction_request", "shift_swap_request", "workflow_log_entry",
)


def reviewed_schema_prefix(db, migration_id):
    tables = REVIEWED_SCHEMA_TABLES
    placeholders = ",".join("?" for _ in tables)
    objects = [list(row) for row in db.execute(
        f"SELECT type,name,tbl_name,sql FROM sqlite_schema WHERE sql IS NOT NULL AND (name IN ({placeholders}) OR tbl_name IN ({placeholders})) ORDER BY type,name",
        (*tables, *tables))]
    details = []
    for table in tables:
        columns = [list(row) for row in db.execute(f"PRAGMA table_xinfo({quoted(table)})")]
        foreign_keys = [list(row) for row in db.execute(f"PRAGMA foreign_key_list({quoted(table)})")]
        indexes = [list(row) for row in db.execute(f"PRAGMA index_list({quoted(table)})")]
        index_details = []
        for index in indexes:
            index_name = index[1].replace('"', '""')
            index_details.append({"row": index, "columns": [list(row) for row in
                                 db.execute(f"PRAGMA index_xinfo(\"{index_name}\")")]})
        details.append({"table": table, "columns": columns, "foreignKeys": foreign_keys,
                        "indexes": index_details})
    return {"migrationId": migration_id, "schemaObjects": objects, "tables": details}


def require_reviewed_schema(db, transition, prefix):
    require(0 <= prefix < len(transition.schema_prefix_contracts), "Invalid reviewed schema prefix")
    contract = transition.schema_prefix_contracts[prefix]
    actual = reviewed_schema_prefix(db, contract["migrationId"])
    normalize = lambda rows: {(row[0], row[1]): (row[0], row[1], row[2], canonical_sql(row[3])) for row in rows}
    expected_objects = normalize(contract["schemaObjects"])
    actual_objects = normalize(actual["schemaObjects"])
    if actual_objects != expected_objects or actual["tables"] != contract["tables"]:
        object_delta = sorted(set(expected_objects) ^ set(actual_objects))
        object_changed = sorted(key for key in expected_objects.keys() & actual_objects.keys()
                                if expected_objects[key] != actual_objects[key])
        expected_tables = {item["table"]: item for item in contract["tables"]}
        actual_tables = {item["table"]: item for item in actual["tables"]}
        table_changed = sorted(name for name in expected_tables.keys() & actual_tables.keys()
                               if expected_tables[name] != actual_tables[name])
        raise MaintenanceError(f"SQLite affected schema differs from reviewed schema prefix {prefix}: "
                               f"objects={object_delta + object_changed}, tables={table_changed}")


def canonical_sql(sql):
    result = []
    in_string = False
    pending_space = False
    index = 0
    while index < len(sql):
        char = sql[index]
        if in_string:
            result.append(char)
            if char == "'":
                if index + 1 < len(sql) and sql[index + 1] == "'":
                    result.append("'")
                    index += 1
                else:
                    in_string = False
        elif char == "'":
            if pending_space and result:
                result.append(" ")
            pending_space = False
            in_string = True
            result.append(char)
        elif char == '"':
            match = re.match(r'"([A-Za-z_][A-Za-z0-9_]*)"', sql[index:])
            if match:
                if pending_space and result:
                    result.append(" ")
                pending_space = False
                result.append(match.group(1))
                index += len(match.group(0)) - 1
            else:
                if pending_space and result:
                    result.append(" ")
                pending_space = False
                result.append(char)
        elif char.isspace():
            pending_space = True
        else:
            if pending_space and result:
                result.append(" ")
            pending_space = False
            result.append(char)
        index += 1
    return "".join(result).strip()


def unaffected_schema(db):
    tables = set(REVIEWED_SCHEMA_TABLES)
    return {(kind, name): sql for kind, name, table, sql in db.execute(
        "SELECT type,name,tbl_name,sql FROM sqlite_schema WHERE sql IS NOT NULL")
        if name not in tables and table not in tables and name != LOCK}


def verify_reviewed_new_values(db, transition, prefix):
    for introduced, table, column, expected in transition.expected_new_values:
        if introduced > prefix:
            continue
        names = {row[1] for row in db.execute(f"PRAGMA table_xinfo({quoted(table)})")}
        require(column in names, f"Reviewed field {table}.{column} is missing at prefix {prefix}")
        require(not db.execute(f"SELECT 1 FROM {quoted(table)} WHERE {quoted(column)} IS NOT ? LIMIT 1",
                               (expected,)).fetchone(),
                f"Reviewed field {table}.{column} has an unexpected value")


def sequence_values(db):
    if not db.execute("SELECT 1 FROM sqlite_schema WHERE type='table' AND name='sqlite_sequence'").fetchone():
        return {}
    rows = list(db.execute("SELECT name,seq FROM sqlite_sequence ORDER BY name"))
    values = dict(rows)
    require(len(values) == len(rows), "Duplicate SQLite sequence rows")
    return values


def reviewed_correction_sequence(before, after, prefix):
    table = "shift_correction_request"
    original, current = sequence_values(before), sequence_values(after)
    previous_high, current_high = original.pop(table, None), current.pop(table, None)
    require(original == current, "Unexpected SQLite sequence changed during the reviewed migration")
    if current_high != previous_high:
        # EF copies the existing rows into a fresh AUTOINCREMENT table. Its sequence
        # then reflects the largest copied ID (zero when the INSERT SELECT was empty).
        rebuilt_high, = after.execute("SELECT MAX(0, COALESCE(MAX(id), 0)) FROM shift_correction_request").fetchone()
        require(prefix >= 4 and current_high == rebuilt_high,
                "SQLite sequence state changed outside the reviewed correction rebuild")
    return previous_high, current_high


def preserve_reviewed_correction_sequence(database_path, previous_high, current_high):
    if previous_high is None or current_high is None or current_high >= previous_high:
        return
    with closing(open_db(database_path, "rw")) as db, db:
        updated = db.execute("UPDATE sqlite_sequence SET seq=? WHERE name='shift_correction_request' AND seq=?",
                             (previous_high, current_high))
        require(updated.rowcount == 1 and sequence_values(db).get("shift_correction_request") == previous_high,
                "Reviewed correction sequence changed before its high-water mark could be restored")
    print(f"Preserved reviewed correction sequence high-water mark: {current_high} -> {previous_high}", flush=True)


def expected_schema(before, columns):
    expected = before.copy()
    with closing(sqlite3.connect(":memory:")) as scratch:
        for table in sorted({entry[0] for entry in columns}):
            require(("table", table) in before, "Reviewed table is missing")
            scratch.execute(before["table", table])
        for table, name, affinity in columns:
            scratch.execute(f"ALTER TABLE {quoted(table)} ADD COLUMN {quoted(name)} {affinity} NULL")
        expected.update(schema(scratch))
    return expected


def row_digest(db, table, fields):
    # Include order by storage class and byte representation, so collation and physical layout
    # do not affect the comparison. Keep employee data out of reports.
    names = list(map(quoted, fields))
    order = ", ".join(f"typeof({name}), {name} COLLATE BINARY" for name in names)
    digest = hashlib.sha256()
    deadline = time.monotonic() + 120
    db.set_progress_handler(lambda: int(time.monotonic() >= deadline), 10000)
    try:
        for row in db.execute(f"SELECT {', '.join(names)} FROM {quoted(table)} ORDER BY {order}"):
            digest.update(repr(row).encode("utf-8"))
            digest.update(b"\n")
    finally:
        db.set_progress_handler(None, 0)
    return digest.digest()


def verify_migration(snapshot_path, clone, before_ids, target_ids, columns):
    transition = columns if isinstance(columns, ReviewedReleaseTransition) else None
    if transition:
        require(len(transition.schema_prefix_contracts) == len(transition.migrations) + 1,
                "Reviewed release requires all eight immutable schema-prefix contracts")
        require(before_ids and transition.schema_prefix_contracts[0]["migrationId"] == before_ids[-1]
                and target_ids[-1] == transition.schema_prefix_contracts[-1]["migrationId"],
                "Migration history does not match the reviewed schema prefixes")
        columns = transition.nullable_columns
    with closing(open_db(snapshot_path, "ro")) as before, closing(open_db(clone, "ro")) as after:
        # Keep sorter output containing employee data out of host temporary files.
        before.execute("PRAGMA temp_store=MEMORY")
        after.execute("PRAGMA temp_store=MEMORY")
        prior_history = verify(before, before_ids)
        history = verify(after, target_ids)
        require(history[:len(prior_history)] == prior_history, "Existing migration history changed")
        before_schema, after_schema = schema(before), schema(after)
        # EF can initialize its own empty migration lock table on first use.
        if ("table", LOCK) not in before_schema:
            after_schema.pop(("table", LOCK), None)
        if transition:
            require_reviewed_schema(before, transition, 0)
            require_reviewed_schema(after, transition, len(transition.migrations))
            require(unaffected_schema(before) == unaffected_schema(after),
                    "An unaffected SQLite schema object changed during migration")
            verify_reviewed_new_values(after, transition, len(transition.migrations))
            require(not before.execute("PRAGMA foreign_key_check").fetchall()
                    and not after.execute("PRAGMA foreign_key_check").fetchall(),
                    "Foreign-key validation failed during reviewed migration")
        else:
            require(expected_schema(before_schema, columns) == after_schema,
                    "SQLite schema differs from the reviewed column additions")
        for kind, table in before_schema:
            if kind != "table" or table == HISTORY or (transition and table == "sqlite_sequence"):
                continue
            fields = before.execute(f"PRAGMA table_xinfo({quoted(table)})").fetchall()
            actual = after.execute(f"PRAGMA table_xinfo({quoted(table)})").fetchall()
            if not transition:
                additions = [(len(fields) + i, name, affinity, 0, None, 0, 0)
                             for i, (_, name, affinity) in enumerate(item for item in columns if item[0] == table)]
                require(actual == fields + additions, "SQLite column metadata differs from review")
                for _, name, *_ in additions:
                    require(not after.execute(f"SELECT 1 FROM {quoted(table)} WHERE {quoted(name)} IS NOT NULL LIMIT 1")
                            .fetchone(), "A new nullable column contains unexpected values")
            else:
                actual_names = {field[1] for field in actual}
                require({field[1] for field in fields} <= actual_names,
                        "A pre-existing SQLite column disappeared during the reviewed migration")
            names = [field[1] for field in fields]
            require(row_digest(before, table, names) == row_digest(after, table, names),
                    "Existing SQLite rows changed during rehearsal")
        if transition:
            sequence = reviewed_correction_sequence(before, after, len(transition.migrations))
    if transition:
        preserve_reviewed_correction_sequence(clone, *sequence)


def verify_no_data_drift(backup, database, columns=(), target_ids=None):
    """Refuse automatic restore if rows might have changed outside the migration."""
    transition = columns if isinstance(columns, ReviewedReleaseTransition) else None
    with closing(open_db(backup, "ro")) as before, closing(open_db(database, "ro")) as after:
        before.execute("PRAGMA temp_store=MEMORY")
        after.execute("PRAGMA temp_store=MEMORY")
        before_schema, after_schema = schema(before), schema(after)
        if ("table", LOCK) not in before_schema:
            after_schema.pop(("table", LOCK), None)
        query = f'SELECT MigrationId, ProductVersion FROM "{HISTORY}" ORDER BY MigrationId'
        original_history, current_history = tuple(before.execute(query)), tuple(after.execute(query))
        if transition:
            require(len(transition.schema_prefix_contracts) == len(transition.migrations) + 1,
                    "Reviewed release recovery requires all eight schema-prefix contracts")
            original_ids = tuple(row[0] for row in original_history)
            verify(before, original_ids)
            require(transition.schema_prefix_contracts[0]["migrationId"] == original_ids[-1],
                    "Recovery backup does not match the reviewed baseline prefix")
            prefix = len(current_history) - len(original_history)
            require(0 <= prefix <= len(transition.migrations),
                    "Database history is outside the reviewed migration prefixes")
            expected_ids = original_ids + transition.migrations[:prefix]
            require(tuple(row[0] for row in current_history) == expected_ids
                    and current_history[:len(original_history)] == original_history,
                    "SQLite history changed outside the reviewed migrations")
            verify(after, expected_ids)
            require_reviewed_schema(before, transition, 0)
            require_reviewed_schema(after, transition, prefix)
            require(unaffected_schema(before) == unaffected_schema(after),
                    "An unaffected SQLite schema object changed after the recovery checkpoint")
            verify_reviewed_new_values(after, transition, prefix)
            require(not before.execute("PRAGMA foreign_key_check").fetchall()
                    and not after.execute("PRAGMA foreign_key_check").fetchall(),
                    "Foreign-key validation failed during recovery preflight")
            for kind, table in before_schema:
                if kind != "table" or table in {HISTORY, LOCK, "sqlite_sequence"}:
                    continue
                names = [row[1] for row in before.execute(f"PRAGMA table_xinfo({quoted(table)})")]
                updated = {row[1] for row in after.execute(f"PRAGMA table_xinfo({quoted(table)})")}
                require(set(names) <= updated, "A pre-existing SQLite column disappeared before recovery")
                require(row_digest(before, table, names) == row_digest(after, table, names),
                        "Database writes may have occurred since the final backup")
            reviewed_correction_sequence(before, after, prefix)
            return prefix
        expected_ids = tuple(row[0] for row in original_history) if target_ids is None else target_ids
        require(len(current_history) >= len(original_history) and current_history[:len(original_history)] == original_history
                and tuple(row[0] for row in current_history) == expected_ids[:len(current_history)],
                "SQLite history changed outside the reviewed migrations")
        present = []
        for kind, table in before_schema:
            if kind == "table" and table not in {HISTORY, LOCK}:
                names = [row[1] for row in before.execute(f"PRAGMA table_xinfo({quoted(table)})")]
                updated = [row[1] for row in after.execute(f"PRAGMA table_xinfo({quoted(table)})")]
                for item in columns:
                    if item[0] == table and item[1] not in names and item[1] in updated:
                        present.append(item)
                        require(not after.execute(f"SELECT 1 FROM {quoted(table)} "
                                                  f"WHERE {quoted(item[1])} IS NOT NULL LIMIT 1").fetchone(),
                                "There is data in a new column. Automatic recovery would discard it.")
                require(row_digest(before, table, names) == row_digest(after, table, names),
                        "Database writes may have occurred since the final backup")
        require(expected_schema(before_schema, present) == after_schema,
                "Database schema changed outside the reviewed migration")


def image_id(command, source, bundle, target):
    path = bundle / "app.iid"
    command("docker", "build", "--iidfile", str(path), "--label", f"org.opencontainers.image.revision={target}",
            str(source), timeout=1800)
    image = path.read_text(encoding="ascii").strip()
    require(re.fullmatch(r"sha256:[0-9a-f]{64}", image), "Expected an immutable candidate Docker image ID")
    return image


def probe(command, inspect, name):
    deadline = time.monotonic() + 120
    while time.monotonic() < deadline:
        state = inspect("container", "inspect", name)[0]["State"]
        require(state["Running"], "Candidate stopped before rehearsal health passed")
        if state["Health"]["Status"] == "healthy":
            response = command("docker", "exec", name, "curl", "-fsS", "--max-time", "3",
                               "http://127.0.0.1:8080/api/health", timeout=10)
            health = json.loads(response)
            require(isinstance(health, dict) and health.get("status") == "ok" and health.get("canConnect") is True,
                    "Candidate rehearsal health failed")
            return
        time.sleep(1)
    raise MaintenanceError("Candidate rehearsal health timed out")


def start_clone(command, inspect, name, image, data, *, require_existing=True, mode=None):
    uid = (os.getuid() or 1654) if hasattr(os, "getuid") else 1654
    gid = (os.getgid() or 1654) if hasattr(os, "getgid") else 1654
    if hasattr(os, "geteuid") and os.geteuid() == 0:
        os.chown(data, uid, gid)
        if (data / "SQLite.db").exists():
            os.chown(data / "SQLite.db", uid, gid)
    environment = {
        "ASPNETCORE_ENVIRONMENT": "Production", "DOTNET_ENVIRONMENT": "Production",
        "ConnectionStrings__Default": "Data Source=/data/SQLite.db",
        "GF3_TRUSTED_PROXY_CIDR": "172.31.254.0/24", "AllowedHosts": "localhost;127.0.0.1",
        "DatabaseStartup__RequireExistingDatabase": str(require_existing).lower(),
        "DatabaseStartup__BackupBeforeMigrate": "true", "Push__Enabled": "false",
        "Jwt__SigningKey": secrets.token_hex(48),
    }
    arguments = [f"--gf3-maintenance-{mode}"] if mode else []
    readonly = ",readonly" if mode == "health" else ""
    command("docker", "create", "--name", name, "--pull", "never", "--network", "none",
            "--cap-drop", "ALL", "--security-opt", "no-new-privileges", "--log-driver", "none",
            "--read-only", "--tmpfs", "/tmp:rw,noexec,nosuid,nodev", "--user", f"{uid}:{gid}",
            "--health-interval", "1s", "--health-timeout", "4s",
            "--mount", f"type=bind,source={data},destination=/data{readonly}",
            *(arg for key, value in environment.items() for arg in ("--env", f"{key}={value}")), image, *arguments)
    config, = inspect("container", "inspect", name)
    host = config["HostConfig"]
    mounts = config["Mounts"]
    require(config["Image"] == image and host["NetworkMode"] == "none" and not host["PortBindings"]
            and not host["Privileged"] and host["ReadonlyRootfs"] and set(host["CapDrop"]) == {"ALL"}
            and len(mounts) == 1 and mounts[0]["Source"] == str(data) and mounts[0]["Destination"] == "/data"
            and mounts[0]["Type"] == "bind" and mounts[0]["RW"] == (mode != "health"),
            "Candidate container is not isolated from production")
    command("docker", "start", name)
    if mode == "migrate":
        require(command("docker", "wait", name, timeout=120).strip() == "0", "Isolated EF migration failed")
    else:
        probe(command, inspect, name)


def remove_clone(command, name):
    ids = command("docker", "ps", "-aq", "--filter", f"name=^/{name}$").splitlines()
    require(len(ids) <= 1 and all(re.fullmatch(r"[0-9a-f]{12,64}", item) for item in ids),
            "Could not identify the disposable rehearsal container")
    if ids:
        command("docker", "rm", "-f", ids[0])


def rehearse(command, inspect, image, bundle, snapshot_path, before_ids, target_ids, columns, *, verify_modes=False):
    data = Path(tempfile.mkdtemp(prefix="clone-", dir=bundle))
    clone = data / "SQLite.db"
    name = "gf3-db-rehearsal-" + secrets.token_hex(12)
    try:
        with closing(open_db(snapshot_path, "ro")) as snapshot_db:
            verify(snapshot_db, before_ids)
        shutil.copyfile(snapshot_path, clone)
        clone.chmod(0o600)
        snapshot_media = Path(snapshot_path).parent / MEDIA_DIRECTORY
        if snapshot_media.exists():
            shutil.copytree(snapshot_media, data / MEDIA_DIRECTORY, copy_function=shutil.copyfile)
            require(media_stamp(snapshot_media) == media_stamp(data / MEDIA_DIRECTORY), "Rehearsal media clone differs from its snapshot")
        else:
            (data / MEDIA_DIRECTORY).mkdir(mode=0o700)
        before_media = media_stamp(data / MEDIA_DIRECTORY)
        if verify_modes:
            start_clone(command, inspect, name, image, data, mode="migrate")
            remove_clone(command, name)
            checkpoint_for_read_only_health(clone)
            start_clone(command, inspect, name, image, data, mode="health")
            # A legacy image that ignores the mode must not qualify for the live path.
            code = command("docker", "exec", name, "curl", "-sS", "-o", "/dev/null", "-w", "%{http_code}",
                           "--max-time", "5", "http://127.0.0.1:8080/")
            require(code == "404", "Maintenance verification host serves ordinary application routes")
        else:
            start_clone(command, inspect, name, image, data)
        command("docker", "stop", "--time", "15", name, timeout=25)
        verify_migration(snapshot_path, clone, before_ids, target_ids, columns)
        require(media_stamp(data / MEDIA_DIRECTORY) == before_media, "Migration rehearsal changed committed media")
    finally:
        # If Docker cannot prove removal, retain its private bind source for diagnosis.
        remove_clone(command, name)
        shutil.rmtree(data)
