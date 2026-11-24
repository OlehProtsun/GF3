# GF3 third-party licenses

Status as of 2026-11-23 for the local changes based on `dev22.11.2026` at `b99fe90a098d6be68d0cbe33e75265aee972bab2`. **License gate blocked.** The inventory records declared licenses and verified exceptions. It is not a clearance for the final image or all shipped assets.

## License changes and release gates

| Item | Evidence and disposition |
| --- | --- |
| SixLabors.ImageSharp 3.2.0 | Removed from the direct Web API dependency and resolved graph. Its Release build requires a license input. [Exact package](https://www.nuget.org/packages/SixLabors.ImageSharp/3.2.0) |
| SkiaSharp 4.157.0 | MIT managed package plus MIT Win32, macOS and Linux.NoDependencies packages. The latter adds no fontconfig dependency. All three native archives carried identical third-party notices (SHA-256 `243f2276bde636931b1cf8517fb11c0451be857996fbe1a2c690a3c7d39502ac`). Publish copies the original MIT license and native notices to `licenses/SkiaSharp/`. The native component review below still applies. [Exact managed package](https://www.nuget.org/packages/SkiaSharp/4.157.0) · [Exact Linux package](https://www.nuget.org/packages/SkiaSharp.NativeAssets.Linux.NoDependencies/4.157.0) |
| WebPush 1.0.13 | MPL-2.0. Replaced with MIT Lib.Net.Http.WebPush 3.3.1 and its MIT Lib.Net.Http.EncryptedContentEncoding 2.1.1 for the resolved .NET target. [Removed package source license](https://github.com/web-push-libs/web-push-csharp/blob/5f707831ebb2088561a0a4324facced6126e3fa7/LICENSE) · [Replacement package](https://www.nuget.org/packages/Lib.Net.Http.WebPush/3.3.1) · [Encoding package](https://www.nuget.org/packages/Lib.Net.Http.EncryptedContentEncoding/2.1.1) |
| PDFsharp 6.2.4 | Verified MIT from the exact NuGet manifest. Version pinned as before. Carry the original MIT terms and copyright when distributing the DLL. [Exact package](https://www.nuget.org/packages/PDFsharp/6.2.4) |
| ws 7.5.14 | MIT, transitive through SignalR. Updated from 7.5.10 within SignalR's declared range to clear the npm runtime advisory. [Exact package](https://registry.npmjs.org/ws/7.5.14) · [Advisory](https://github.com/advisories/GHSA-96hv-2xvq-fx4p) |
| SixLabors.Fonts 1.0.0 | The resolved package declares Apache-2.0. It is pulled in by ClosedXML as a transitive dependency. This entry concerns the resolved version only. Keep its Apache terms and upstream notices. [Exact package](https://www.nuget.org/packages/SixLabors.Fonts/1.0.0) · [ClosedXML 0.105.0](https://www.nuget.org/packages/ClosedXML/0.105.0) |
| gsap 3.15.0 and @gsap/react 2.1.2 | Both installed manifests reference the custom GSAP Standard license. Webflow states that commercial use is available at no charge. Its product terms §9 cover use in web applications, restrict competing visual animation builders and require preservation of notices. GF3 uses GSAP to animate the application and its promo, so the observed use fits the stated permitted category by inference. The terms contain no library fee triggered by GF3 charging for schedules. This remains a non-permissive dependency and an exception to the permissive-only preference. Replacing it requires preserving animation timing, cleanup, reduced motion and promo seeking/capture. [Webflow product terms §9](https://webflow.com/legal/product-terms#9-gsap-software-license-agreement) · [Commercial-use announcement](https://webflow.com/updates/gsap-becomes-free) · [React integration](https://github.com/greensock/react#license) |
| MPL-2.0 npm development packages | The lock includes MPL-2.0 CSS tooling used through jsdom and its dependencies. These entries are marked development-only below. A strict permissive-only rule extending to build and test tooling requires an approved replacement of the relevant test/CSS toolchain and equivalent coverage. These entries remain a policy blocker under that interpretation. [MPL-2.0](https://www.mozilla.org/MPL/2.0/) |
| Native SQLite | SourceGear.sqlite3 3.50.4.5 package LICENSE.txt declares SQLite public domain. Managed SQLitePCLRaw uses Apache-2.0. Preserve the package notice and Apache text. [SQLite copyright](https://www.sqlite.org/copyright.html) · [Exact native package](https://www.nuget.org/packages/SourceGear.sqlite3/3.50.4.5) |
| Carlito Regular 1.104 | Bundled the unmodified font and upstream SIL OFL 1.1 text. Replaces the Windows Calibri default and the Linux system-font dependency. OFL permits commercial use and embedding with its terms, including the reserved name. Preserve `GF3.WebApi/Resources/Fonts/Carlito-OFL.txt` in the release. The font file SHA-256 is `f6418f708baede9789daef5d458c0f53d2a888af9820e8062934e504fedc6595`. [Exact font and OFL](https://github.com/google/fonts/tree/605c9f29dd5770762bd7ee8ea8ac4ae7664b2b7b/ofl/carlito) |
| Production Linux base and system libraries | `mcr.microsoft.com/dotnet/aspnet:10.0` and the installed apt package versions can change between builds. The local Linux amd64 image and exact OS notice inventory are below. The image includes system components such as LGPL glibc. They impose no library fee. A permissive-only policy for the entire OS image requires a separate runtime and base-image decision. [Official .NET Docker images](https://github.com/dotnet/dotnet-docker) · [glibc terms](https://sourceware.org/glibc/) |

## Native components and notices

The SkiaSharp 4.157.0 NuGet repository manifest pins SkiaSharp commit `2a53a3c8641073bc914c63ee158bbf62b8a0bb92` and the native Skia submodule at `51cb594d773b5383a45065ea3c8cbec398ad2c36`. The following source versions come from the exact upstream component manifest. It is a source inventory, and may list components disabled in particular binary variants. Confirm remaining per-RID provenance against the built image and platform binaries before clearing the gate. [Component manifest](https://github.com/mono/SkiaSharp/blob/2a53a3c8641073bc914c63ee158bbf62b8a0bb92/cgmanifest.json) · [Linux build flags](https://github.com/mono/SkiaSharp/blob/2a53a3c8641073bc914c63ee158bbf62b8a0bb92/native/linux/build.cake)

| Component | Version | License and evidence |
| --- | --- | --- |
| Skia | `51cb594d773b5383a45065ea3c8cbec398ad2c36` | BSD-3-Clause. [Source](https://github.com/mono/skia/tree/51cb594d773b5383a45065ea3c8cbec398ad2c36) |
| libjpeg-turbo | 3.1.4.1 / `9217719d3a58633923b096af4c1d50d304768a64` | IJG, modified BSD and zlib terms. Include the acknowledgments and original notices. [Exact source](https://github.com/libjpeg-turbo/libjpeg-turbo/tree/9217719d3a58633923b096af4c1d50d304768a64) |
| libpng | 1.6.58 / `3061454d980de7d53608f594194cfac722721d2a` | libpng permissive license. [Exact source](https://skia.googlesource.com/third_party/libpng/+/3061454d980de7d53608f594194cfac722721d2a) |
| libwebp | 1.6.0 / `4fa21912338357f89e4fd51cf2368325b59e9bd9` | BSD-3-Clause. Preserve upstream patent grant text alongside notices. [Exact source](https://chromium.googlesource.com/webm/libwebp/+/4fa21912338357f89e4fd51cf2368325b59e9bd9/) |
| zlib | 1.3.2.1-motley | Zlib. Chromium fork. [Component manifest](https://github.com/mono/SkiaSharp/blob/2a53a3c8641073bc914c63ee158bbf62b8a0bb92/cgmanifest.json) |
| Wuffs | 0.3.3+3399.20230408 / `e3f919ccfe3ef542cfc983a82146070258fb57f8` | Apache-2.0 or MIT. The pinned Skia source enables Wuffs and lists `SkWuffsCodec.cpp` for GIF. The generic NuGet notice also contains a legacy GIF MPL/GPL/LGPL notice. Keep the complete supplier notice and confirm compiled components. GF3 GIF uploads use the existing independent parser. [Skia flags](https://github.com/mono/skia/blob/51cb594d773b5383a45065ea3c8cbec398ad2c36/gn/skia.gni) · [Wuffs](https://github.com/google/wuffs) |
| FreeType | 2.14.3 | Use the permissive FreeType License option and its acknowledgment. [FreeType license](https://freetype.org/license.html) |
| Expat, Brotli, HarfBuzz | 2.8.1, 1.2.0, 14.4.0 | MIT, MIT, and Old MIT respectively. Linux build disables HarfBuzz and ICU. [Exact component manifest](https://github.com/mono/SkiaSharp/blob/2a53a3c8641073bc914c63ee158bbf62b8a0bb92/cgmanifest.json) |

The framework-dependent candidate publish includes SkiaSharp's MIT license and complete supplier notice. On the cached Ubuntu 24.04 test image, `ldd` on the published Linux x64 binary resolved glibc 2.39-0ubuntu8.9 and GCC runtime libraries 14.2.0-4ubuntu2~24.04.1. This check is limited to the test image and this architecture. The final image comparison below identifies the actual production-image package versions. Statically compiled components for each distributed RID remain open.

The published SkiaSharp 4.157.0 files contain `SkWuffsCodec` in all 13 Linux binaries, the macOS universal binary and all three Windows binaries. All Linux files and Windows PDBs also contain `wuffs_gif`. The inspected files did not contain a `SkGifCodec` marker. This corroborates upstream's Wuffs configuration and identifies the origin of the active GIF codec. Symbol and string checks alone do not establish the full set of statically linked objects. Retain the supplier's complete native notice. [Pinned codec source and flags](https://github.com/mono/skia/blob/51cb594d773b5383a45065ea3c8cbec398ad2c36/gn/skia.gni)

## Production Linux amd64 image

Local production Dockerfile build `sha256:fcdb3615fc8de5bfe540dd42c1bc1aa8791c0524639ba0f819e8362a2484f193` uses Ubuntu 24.04.5 LTS, Microsoft.NETCore.App 10.0.15 and Microsoft.AspNetCore.App 10.0.15. The Linux amd64 ASP.NET base manifest is `sha256:f7b58ba6e8f2cbe11c3cb58e8a5f5f2a077834095e24f21e6eb969ab4f9eb4bf`. Preserve the .NET distribution's original terms and third-party notices. [Runtime source](https://github.com/dotnet/runtime/tree/v10.0.15) · [ASP.NET source](https://github.com/dotnet/aspnetcore/tree/v10.0.15)

All 112 installed OS packages expose a readable notice in the image, including package paths that resolve to a shared notice. The follow-up image `sha256:feee34343e538a578cc16dba2597cc2c7133ef0de121f2f3960239ad6739c8be` had the same 112 package names and versions in `dpkg-query -W`. The terms below summarize the notices. The source links identify the exact Ubuntu source revision. Preserve the original `/usr/share/doc/<package>/copyright` and `/usr/share/common-licenses` contents. The installed image includes GPL and LGPL components, so a permissive-only image remains a separate release blocker. Other architectures and future floating base digests require their own inventory.

| Installed OS package | Binary version | License labels or notice summary | Exact Ubuntu source |
| --- | --- | --- | --- |
| `apt` | 2.8.4ubuntu0.1 | BSD-3-clause, Expat, GPL-2, GPL-2+ | [apt 2.8.4ubuntu0.1](https://launchpad.net/ubuntu/+source/apt/2.8.4ubuntu0.1) |
| `base-files` | 13ubuntu10.5 | GPL-2.0-or-later | [base-files 13ubuntu10.5](https://launchpad.net/ubuntu/+source/base-files/13ubuntu10.5) |
| `base-passwd` | 3.6.3build1 | GPL-2, public-domain | [base-passwd 3.6.3build1](https://launchpad.net/ubuntu/+source/base-passwd/3.6.3build1) |
| `bash` | 5.2.21-2ubuntu4 | BSD-4-clause-UC, BSD-4-clause-UC and MIT-like, GFDL-NIV-1.3, GPL-2+, GPL-3+, GPL-3+ with Bison exception, Latex2e, MIT-like, permissive | [bash 5.2.21-2ubuntu4](https://launchpad.net/ubuntu/+source/bash/5.2.21-2ubuntu4) |
| `bsdutils` | 1:2.39.3-9ubuntu6.6 | BSD-3-clause, BSD-4-clause, BSLA, GPL-2, GPL-2+, GPL-3+, LGPL, LGPL-2+, LGPL-2.1+, LGPL-3+, MIT, public-domain | [util-linux 2.39.3-9ubuntu6.6](https://launchpad.net/ubuntu/+source/util-linux/2.39.3-9ubuntu6.6) |
| `ca-certificates` | 20260816ubuntu0.24.04.2 | GPL-2+, MPL-2.0 | [ca-certificates 20260816ubuntu0.24.04.2](https://launchpad.net/ubuntu/+source/ca-certificates/20260816ubuntu0.24.04.2) |
| `coreutils` | 9.4-3ubuntu6.3 | BSD-4-clause-UC, FSFULLR, GFDL-NIV-1.3, GPL-3+, GPL-3+ and BSD-4-clause-UC, GPL-3+ and ISC, ISC | [coreutils 9.4-3ubuntu6.3](https://launchpad.net/ubuntu/+source/coreutils/9.4-3ubuntu6.3) |
| `curl` | 8.5.0-2ubuntu10.16 | BSD-3-Clause, BSD-3-clause, BSD-4-Clause-UC, FSFULLR, GPL-2+ with Autoconf-data exception, GPL-2+ with Libtool exception, GPL-3+ with Autoconf-data exception, ISC, OLDAP-2.8, X11, curl | [curl 8.5.0-2ubuntu10.16](https://launchpad.net/ubuntu/+source/curl/8.5.0-2ubuntu10.16) |
| `dash` | 0.5.12-6ubuntu5 | BSD-3-Clause, BSD-3-clause, GPL-2+, public-domain | [dash 0.5.12-6ubuntu5](https://launchpad.net/ubuntu/+source/dash/0.5.12-6ubuntu5) |
| `debconf` | 1.5.86ubuntu1 | BSD-2-clause | [debconf 1.5.86ubuntu1](https://launchpad.net/ubuntu/+source/debconf/1.5.86ubuntu1) |
| `debianutils` | 5.17build1 | GPL-2+, SMAIL-GPL, public-domain | [debianutils 5.17build1](https://launchpad.net/ubuntu/+source/debianutils/5.17build1) |
| `diffutils` | 1:3.10-1ubuntu0.1 | FSFAP, FSFULLR, GFDL-NIV-1.3, GPL-2+, GPL-3+, GPL-3+ and FSFULLR, GPL-3+ with autoconf exception, GPL-3+ with texinfo exception, LGPL-2.0+, LGPL-2.1+, LGPL-3.0+, LGPL-3.0+ or GPL-2+, X11, public-domain | [diffutils 1:3.10-1ubuntu0.1](https://launchpad.net/ubuntu/+source/diffutils/1%3A3.10-1ubuntu0.1) |
| `dpkg` | 1.22.6ubuntu6.6 | GPL-2+, public-domain-s-s-d | [dpkg 1.22.6ubuntu6.6](https://launchpad.net/ubuntu/+source/dpkg/1.22.6ubuntu6.6) |
| `e2fsprogs` | 1.47.0-2.4~exp1ubuntu4.1 | Apache-2, BSD-3-Clause, GPL or MIT-US-export, GPL-2, GPL-2+ with Texinfo exception, ISC, Kazlib, LGPL-2, Latex2e | [e2fsprogs 1.47.0-2.4~exp1ubuntu4.1](https://launchpad.net/ubuntu/+source/e2fsprogs/1.47.0-2.4~exp1ubuntu4.1) |
| `findutils` | 4.9.0-5build1 | BSD-3-clause, BSD-3-clause and/or GPL-3+, FSFAP, FSFULLR, GFDL-NIV-1.3+, GPL with automake exception, GPL-2+, GPL-2+ with Autoconf-data exception, GPL-3+, GPL-3+ with Autoconf-data exception, GPL-3+ with Bison-2.2 exception, ISC, ISC and/or LGPL-2.1+, LGPL-2+, LGPL-2.1+, LGPL-3, LGPL-3+, X11, public-domain | [findutils 4.9.0-5build1](https://launchpad.net/ubuntu/+source/findutils/4.9.0-5build1) |
| `gcc-14-base:amd64` | 14.2.0-4ubuntu2~24.04.1 | GPL-3.0-or-later, GCC Runtime Library Exception 3.1 and additional notices | [gcc-14 14.2.0-4ubuntu2~24.04.1](https://launchpad.net/ubuntu/+source/gcc-14/14.2.0-4ubuntu2~24.04.1) |
| `gpgv` | 2.4.4-2ubuntu17.6 | BSD-3-clause, CC0-1.0, Expat, GPL-2+, GPL-3+, GPL-3+ or BSD-3-clause, LGPL-2.1+, LGPL-3+, LGPL-3+ or GPL-2+, RFC-Reference, TinySCHEME, permissive | [gnupg2 2.4.4-2ubuntu17.6](https://launchpad.net/ubuntu/+source/gnupg2/2.4.4-2ubuntu17.6) |
| `grep` | 3.11-4build1 | GPL-3+ | [grep 3.11-4build1](https://launchpad.net/ubuntu/+source/grep/3.11-4build1) |
| `gzip` | 1.12-1ubuntu3.2 | FSF-manpages, GFDL-1.3+-no-invariant, GPL-3+ | [gzip 1.12-1ubuntu3.2](https://launchpad.net/ubuntu/+source/gzip/1.12-1ubuntu3.2) |
| `hostname` | 3.23+nmu2ubuntu2 | GPL-2 | [hostname 3.23+nmu2ubuntu2](https://launchpad.net/ubuntu/+source/hostname/3.23%2Bnmu2ubuntu2) |
| `init-system-helpers` | 1.66ubuntu1 | BSD-3-clause, GPL-2+ | [init-system-helpers 1.66ubuntu1](https://launchpad.net/ubuntu/+source/init-system-helpers/1.66ubuntu1) |
| `libacl1:amd64` | 2.3.2-1build1.1 | GPL-2+, LGPL-2+ | [acl 2.3.2-1build1.1](https://launchpad.net/ubuntu/+source/acl/2.3.2-1build1.1) |
| `libapt-pkg6.0t64:amd64` | 2.8.4ubuntu0.1 | BSD-3-clause, Expat, GPL-2, GPL-2+ | [apt 2.8.4ubuntu0.1](https://launchpad.net/ubuntu/+source/apt/2.8.4ubuntu0.1) |
| `libassuan0:amd64` | 2.5.6-1build1 | GAP, GAP~FSF, GPL-2+, GPL-2+ with libtool exception, GPL-3+, LGPL-2.1+, LGPL-3+ | [libassuan 2.5.6-1build1](https://launchpad.net/ubuntu/+source/libassuan/2.5.6-1build1) |
| `libattr1:amd64` | 1:2.5.2-1ubuntu0.1 | GPL-2+, LGPL-2+ | [attr 1:2.5.2-1ubuntu0.1](https://launchpad.net/ubuntu/+source/attr/1%3A2.5.2-1ubuntu0.1) |
| `libaudit-common` | 1:3.1.2-2.1ubuntu0.1 | GPL-2, LGPL-2.1 | [audit 1:3.1.2-2.1ubuntu0.1](https://launchpad.net/ubuntu/+source/audit/1%3A3.1.2-2.1ubuntu0.1) |
| `libaudit1:amd64` | 1:3.1.2-2.1ubuntu0.1 | GPL-2, LGPL-2.1 | [audit 1:3.1.2-2.1ubuntu0.1](https://launchpad.net/ubuntu/+source/audit/1%3A3.1.2-2.1ubuntu0.1) |
| `libblkid1:amd64` | 2.39.3-9ubuntu6.6 | BSD-3-clause, BSD-4-clause, BSLA, GPL-2, GPL-2+, GPL-3+, LGPL, LGPL-2+, LGPL-2.1+, LGPL-3+, MIT, public-domain | [util-linux 2.39.3-9ubuntu6.6](https://launchpad.net/ubuntu/+source/util-linux/2.39.3-9ubuntu6.6) |
| `libbrotli1:amd64` | 1.1.0-2ubuntu0.1 | MIT | [brotli 1.1.0-2ubuntu0.1](https://launchpad.net/ubuntu/+source/brotli/1.1.0-2ubuntu0.1) |
| `libbz2-1.0:amd64` | 1.0.8-5.1ubuntu0.1 | BSD-variant, GPL-2 | [bzip2 1.0.8-5.1ubuntu0.1](https://launchpad.net/ubuntu/+source/bzip2/1.0.8-5.1ubuntu0.1) |
| `libc-bin` | 2.39-0ubuntu8.12 | LGPL-2.1-or-later, GPL and BSD notices | [glibc 2.39-0ubuntu8.12](https://launchpad.net/ubuntu/+source/glibc/2.39-0ubuntu8.12) |
| `libc6:amd64` | 2.39-0ubuntu8.12 | LGPL-2.1-or-later, GPL and BSD notices | [glibc 2.39-0ubuntu8.12](https://launchpad.net/ubuntu/+source/glibc/2.39-0ubuntu8.12) |
| `libcap-ng0:amd64` | 0.8.4-2build2 | GPL-2+, GPL-3, LGPL-2.1+ | [libcap-ng 0.8.4-2build2](https://launchpad.net/ubuntu/+source/libcap-ng/0.8.4-2build2) |
| `libcap2:amd64` | 1:2.66-5ubuntu2.4 | BSD-3-clause, BSD-3-clause or GPL-2, BSD-3-clause or GPL-2+, GPL-2, GPL-2+ | [libcap2 1:2.66-5ubuntu2.4](https://launchpad.net/ubuntu/+source/libcap2/1%3A2.66-5ubuntu2.4) |
| `libcom-err2:amd64` | 1.47.0-2.4~exp1ubuntu4.1 | Apache-2, BSD-3-Clause, GPL or MIT-US-export, GPL-2, GPL-2+ with Texinfo exception, ISC, Kazlib, LGPL-2, Latex2e | [e2fsprogs 1.47.0-2.4~exp1ubuntu4.1](https://launchpad.net/ubuntu/+source/e2fsprogs/1.47.0-2.4~exp1ubuntu4.1) |
| `libcrypt1:amd64` | 1:4.4.36-4build1 | LGPL-2.1-or-later, BSD and public-domain notices | [libxcrypt 1:4.4.36-4build1](https://launchpad.net/ubuntu/+source/libxcrypt/1%3A4.4.36-4build1) |
| `libcurl4t64:amd64` | 8.5.0-2ubuntu10.16 | BSD-3-Clause, BSD-3-clause, BSD-4-Clause-UC, FSFULLR, GPL-2+ with Autoconf-data exception, GPL-2+ with Libtool exception, GPL-3+ with Autoconf-data exception, ISC, OLDAP-2.8, X11, curl | [curl 8.5.0-2ubuntu10.16](https://launchpad.net/ubuntu/+source/curl/8.5.0-2ubuntu10.16) |
| `libdb5.3t64:amd64` | 5.3.28+dfsg2-7 | Artistic or BSD-3-clause, BSD-3-clause, BSD-3-clause-fjord, GPL, GPL or Artistic, GPL-3, MIT-old, Ms-PL, Sleepycat, Sleepycat and BSD-3-clause, TCL-like, X11, zlib | [db5.3 5.3.28+dfsg2-7](https://launchpad.net/ubuntu/+source/db5.3/5.3.28%2Bdfsg2-7) |
| `libdebconfclient0:amd64` | 0.271ubuntu3 | BSD-2-Clause, BSD-2-clause, GPL-2+ | [cdebconf 0.271ubuntu3](https://launchpad.net/ubuntu/+source/cdebconf/0.271ubuntu3) |
| `libext2fs2t64:amd64` | 1.47.0-2.4~exp1ubuntu4.1 | Apache-2, BSD-3-Clause, GPL or MIT-US-export, GPL-2, GPL-2+ with Texinfo exception, ISC, Kazlib, LGPL-2, Latex2e | [e2fsprogs 1.47.0-2.4~exp1ubuntu4.1](https://launchpad.net/ubuntu/+source/e2fsprogs/1.47.0-2.4~exp1ubuntu4.1) |
| `libffi8:amd64` | 3.4.6-1build1 | Expat, GPL, GPL-2+, GPL-3+, MPL-1.1 or GPL-2+ or LGPL-2.1+, X11, public-domain | [libffi 3.4.6-1build1](https://launchpad.net/ubuntu/+source/libffi/3.4.6-1build1) |
| `libgcc-s1:amd64` | 14.2.0-4ubuntu2~24.04.1 | GPL-3.0-or-later with GCC Runtime Library Exception 3.1, additional notices | [gcc-14 14.2.0-4ubuntu2~24.04.1](https://launchpad.net/ubuntu/+source/gcc-14/14.2.0-4ubuntu2~24.04.1) |
| `libgcrypt20:amd64` | 1.10.3-2ubuntu0.2 | LGPL-2.1-or-later for library, GPL-2.0-or-later and additional notices | [libgcrypt20 1.10.3-2ubuntu0.2](https://launchpad.net/ubuntu/+source/libgcrypt20/1.10.3-2ubuntu0.2) |
| `libgmp10:amd64` | 2:6.3.0+dfsg-2ubuntu6.1 | GPL-2+, GPL-2+ or LGPL-3+, GPL-3+, GPL-3+ with Bison exception, LGPL-3+ | [gmp 2:6.3.0+dfsg-2ubuntu6.1](https://launchpad.net/ubuntu/+source/gmp/2%3A6.3.0%2Bdfsg-2ubuntu6.1) |
| `libgnutls30t64:amd64` | 3.8.3-1.1ubuntu3.6 | Apache-2.0, BSD-3-Clause, CC0 license, Expat, GPLv3+, LGPLv2.1+, LGPLv3+_or_GPLv2+, The main library is licensed under GNU Lesser | [gnutls28 3.8.3-1.1ubuntu3.6](https://launchpad.net/ubuntu/+source/gnutls28/3.8.3-1.1ubuntu3.6) |
| `libgpg-error0:amd64` | 1.47-3build2.1 | BSD-3-clause, GPL-3+, LGPL-2.1+, LGPL-2.1+ or BSD-3-clause, g10-permissive | [libgpg-error 1.47-3build2.1](https://launchpad.net/ubuntu/+source/libgpg-error/1.47-3build2.1) |
| `libgssapi-krb5-2:amd64` | 1.20.1-6ubuntu2.10 | MIT Kerberos terms, BSD and additional notices | [krb5 1.20.1-6ubuntu2.10](https://launchpad.net/ubuntu/+source/krb5/1.20.1-6ubuntu2.10) |
| `libhogweed6t64:amd64` | 3.9.1-2.2build1.1 | Expat, GAP, GPL-2, GPL-2+, GPL-3+ with Autoconf exception, LGPL-2+, LGPL-3+, LGPL-3+ or GPL-2+, public-domain | [nettle 3.9.1-2.2build1.1](https://launchpad.net/ubuntu/+source/nettle/3.9.1-2.2build1.1) |
| `libicu74:amd64` | 74.2-1ubuntu3.1 | GPL-3, MIT | [icu 74.2-1ubuntu3.1](https://launchpad.net/ubuntu/+source/icu/74.2-1ubuntu3.1) |
| `libidn2-0:amd64` | 2.3.7-2build1.1 | GPL-2+, GPL-3+, LGPL-3+, LGPL-3+ or GPL-2+, Unicode | [libidn2 2.3.7-2build1.1](https://launchpad.net/ubuntu/+source/libidn2/2.3.7-2build1.1) |
| `libk5crypto3:amd64` | 1.20.1-6ubuntu2.10 | MIT Kerberos terms, BSD and additional notices | [krb5 1.20.1-6ubuntu2.10](https://launchpad.net/ubuntu/+source/krb5/1.20.1-6ubuntu2.10) |
| `libkeyutils1:amd64` | 1.6.3-3build1 | GPL-2+, LGPL-2+ | [keyutils 1.6.3-3build1](https://launchpad.net/ubuntu/+source/keyutils/1.6.3-3build1) |
| `libkrb5-3:amd64` | 1.20.1-6ubuntu2.10 | MIT Kerberos terms, BSD and additional notices | [krb5 1.20.1-6ubuntu2.10](https://launchpad.net/ubuntu/+source/krb5/1.20.1-6ubuntu2.10) |
| `libkrb5support0:amd64` | 1.20.1-6ubuntu2.10 | MIT Kerberos terms, BSD and additional notices | [krb5 1.20.1-6ubuntu2.10](https://launchpad.net/ubuntu/+source/krb5/1.20.1-6ubuntu2.10) |
| `libldap2:amd64` | 2.6.10+dfsg-0ubuntu0.24.04.1 | BSD-3-clause, BSD-3-clause-California, BSD-3-clause-variant, BSD-4-clause-California, Beerware, Expat, Expat-ISC, Expat-UNM, F5, FSF-unlimited, FSF-unlimited and GPL-2+ with Autoconf exception, FSF-unlimited and GPL-2+ with Libtool exception, FSF-unlimited and OpenLDAP-2.8, GPL-2+, GPL-2+ with Autoconf exception, GPL-2+ with Libtool exception, GPL-2+ with Libtool exception and GPL-3+ with Libtool exception and GPL-3+, GPL-3+, GPL-3+ with Autoconf exception, GPL-3+ with Libtool exception, JCG, MIT-XC, NeoSoft-permissive, OpenLDAP-2.8, OpenLDAP-2.8 and BSD-3-clause, OpenLDAP-2.8 and BSD-3-clause-variant, OpenLDAP-2.8 and BSD-4-clause-California, OpenLDAP-2.8 and Beerware, OpenLDAP-2.8 and Expat, OpenLDAP-2.8 and Expat-ISC, OpenLDAP-2.8 and Expat-UNM, OpenLDAP-2.8 and FSF-unlimited and GPL-2+ with Libtool exception, OpenLDAP-2.8 and JCG and UMich, OpenLDAP-2.8 and UMich, OpenLDAP-2.8 and UMich and F5, UMich, public-domain | [openldap 2.6.10+dfsg-0ubuntu0.24.04.1](https://launchpad.net/ubuntu/+source/openldap/2.6.10%2Bdfsg-0ubuntu0.24.04.1) |
| `liblz4-1:amd64` | 1.9.4-1build1.1 | BSD-2-clause, GPL-2+, GPL-2+ or BSD-2-clause | [lz4 1.9.4-1build1.1](https://launchpad.net/ubuntu/+source/lz4/1.9.4-1build1.1) |
| `liblzma5:amd64` | 5.6.1+really5.4.5-1ubuntu0.4 | Autoconf, GPL-2, GPL-2+, LGPL-2.1+, PD, PD-debian, config-h, noderivs, none, permissive-fsf, permissive-nowarranty, probably-PD | [xz-utils 5.6.1+really5.4.5-1ubuntu0.4](https://launchpad.net/ubuntu/+source/xz-utils/5.6.1%2Breally5.4.5-1ubuntu0.4) |
| `libmd0:amd64` | 1.1.0-2build1.1 | BSD-2-clause, BSD-2-clause-NetBSD, BSD-3-clause, BSD-3-clause-Aaron-D-Gifford, Beerware, ISC, public-domain-md4, public-domain-md5, public-domain-sha1 | [libmd 1.1.0-2build1.1](https://launchpad.net/ubuntu/+source/libmd/1.1.0-2build1.1) |
| `libmount1:amd64` | 2.39.3-9ubuntu6.6 | BSD-3-clause, BSD-4-clause, BSLA, GPL-2, GPL-2+, GPL-3+, LGPL, LGPL-2+, LGPL-2.1+, LGPL-3+, MIT, public-domain | [util-linux 2.39.3-9ubuntu6.6](https://launchpad.net/ubuntu/+source/util-linux/2.39.3-9ubuntu6.6) |
| `libncursesw6:amd64` | 6.4+20240113-1ubuntu2.2 | BSD-3-clause, MIT/X11, X11 | [ncurses 6.4+20240113-1ubuntu2.2](https://launchpad.net/ubuntu/+source/ncurses/6.4%2B20240113-1ubuntu2.2) |
| `libnettle8t64:amd64` | 3.9.1-2.2build1.1 | Expat, GAP, GPL-2, GPL-2+, GPL-3+ with Autoconf exception, LGPL-2+, LGPL-3+, LGPL-3+ or GPL-2+, public-domain | [nettle 3.9.1-2.2build1.1](https://launchpad.net/ubuntu/+source/nettle/3.9.1-2.2build1.1) |
| `libnghttp2-14:amd64` | 1.59.0-1ubuntu0.4 | BSD-2-clause, Expat, GPL-3+ with autoconf exception, MIT, all-permissive | [nghttp2 1.59.0-1ubuntu0.4](https://launchpad.net/ubuntu/+source/nghttp2/1.59.0-1ubuntu0.4) |
| `libnpth0t64:amd64` | 1.6-3.1build1 | LGPL-2.1+ | [npth 1.6-3.1build1](https://launchpad.net/ubuntu/+source/npth/1.6-3.1build1) |
| `libp11-kit0:amd64` | 0.25.3-4ubuntu2.2 | Apache-2.0, BSD-3-clause, FSFAP, FSFULLR, GPL-2+ with Autoconf-data exception, GPL-3+ with Autoconf-data exception, ISC, LGPL-2.1+, X11, customFSFUL, customFSFULLRWD | [p11-kit 0.25.3-4ubuntu2.2](https://launchpad.net/ubuntu/+source/p11-kit/0.25.3-4ubuntu2.2) |
| `libpam-modules:amd64` | 1.5.3-5ubuntu5.7 | BSD-3-clause or GPL, BSD-tcp_wrappers, Beerware, GPL-2, GPL-2+, GPL-3, GPL-3+ with Bison exception, LGPL-2+, public-domain | [pam 1.5.3-5ubuntu5.7](https://launchpad.net/ubuntu/+source/pam/1.5.3-5ubuntu5.7) |
| `libpam-modules-bin` | 1.5.3-5ubuntu5.7 | BSD-3-clause or GPL, BSD-tcp_wrappers, Beerware, GPL-2, GPL-2+, GPL-3, GPL-3+ with Bison exception, LGPL-2+, public-domain | [pam 1.5.3-5ubuntu5.7](https://launchpad.net/ubuntu/+source/pam/1.5.3-5ubuntu5.7) |
| `libpam-runtime` | 1.5.3-5ubuntu5.7 | BSD-3-clause or GPL, BSD-tcp_wrappers, Beerware, GPL-2, GPL-2+, GPL-3, GPL-3+ with Bison exception, LGPL-2+, public-domain | [pam 1.5.3-5ubuntu5.7](https://launchpad.net/ubuntu/+source/pam/1.5.3-5ubuntu5.7) |
| `libpam0g:amd64` | 1.5.3-5ubuntu5.7 | BSD-3-clause or GPL, BSD-tcp_wrappers, Beerware, GPL-2, GPL-2+, GPL-3, GPL-3+ with Bison exception, LGPL-2+, public-domain | [pam 1.5.3-5ubuntu5.7](https://launchpad.net/ubuntu/+source/pam/1.5.3-5ubuntu5.7) |
| `libpcre2-8-0:amd64` | 10.42-4ubuntu2.2 | BSD-2-clause, BSD-3-clause, BSD-3-clause-Cambridge with BINARY LIBRARY-LIKE PACKAGES exception, X11, public-domain | [pcre2 10.42-4ubuntu2.2](https://launchpad.net/ubuntu/+source/pcre2/10.42-4ubuntu2.2) |
| `libproc2-0:amd64` | 2:4.0.4-4ubuntu3.3 | GPL-2.0+, LGPL-2.0+, LGPL-2.1+ | [procps 2:4.0.4-4ubuntu3.3](https://launchpad.net/ubuntu/+source/procps/2%3A4.0.4-4ubuntu3.3) |
| `libpsl5t64:amd64` | 0.21.2-1.1build1 | Chromium, MIT, gnulib | [libpsl 0.21.2-1.1build1](https://launchpad.net/ubuntu/+source/libpsl/0.21.2-1.1build1) |
| `librtmp1:amd64` | 2.4+20151223.gitfa8646d.1-2build7 | LGPL library, GPL-2.0-or-later tool and additional notices | [rtmpdump 2.4+20151223.gitfa8646d.1-2build7](https://launchpad.net/ubuntu/+source/rtmpdump/2.4%2B20151223.gitfa8646d.1-2build7) |
| `libsasl2-2:amd64` | 2.1.28+dfsg1-5ubuntu3.1 | BSD-2-clause, BSD-2-clause and MIT-CMU, BSD-2.2-clause, BSD-3-Clause-Attribution, BSD-3-Clause-Attribution and IBM-as-is, BSD-3-Clause-Attribution and MIT-Export, BSD-3-clause, BSD-3-clause-JANET, BSD-3-clause-JANET and BSD-3-Clause-Attribution, BSD-3-clause-PADL, BSD-3-clause-PADL and MIT-OpenVision, BSD-4-clause-UC, FSFULLR, FSFULLR and MIT-CMU, GPL-3, GPL-3+, IBM-as-is, MIT-CMU, MIT-Export, MIT-OpenVision, OpenLDAP, RSA-MD | [cyrus-sasl2 2.1.28+dfsg1-5ubuntu3.1](https://launchpad.net/ubuntu/+source/cyrus-sasl2/2.1.28%2Bdfsg1-5ubuntu3.1) |
| `libsasl2-modules-db:amd64` | 2.1.28+dfsg1-5ubuntu3.1 | BSD-2-clause, BSD-2-clause and MIT-CMU, BSD-2.2-clause, BSD-3-Clause-Attribution, BSD-3-Clause-Attribution and IBM-as-is, BSD-3-Clause-Attribution and MIT-Export, BSD-3-clause, BSD-3-clause-JANET, BSD-3-clause-JANET and BSD-3-Clause-Attribution, BSD-3-clause-PADL, BSD-3-clause-PADL and MIT-OpenVision, BSD-4-clause-UC, FSFULLR, FSFULLR and MIT-CMU, GPL-3, GPL-3+, IBM-as-is, MIT-CMU, MIT-Export, MIT-OpenVision, OpenLDAP, RSA-MD | [cyrus-sasl2 2.1.28+dfsg1-5ubuntu3.1](https://launchpad.net/ubuntu/+source/cyrus-sasl2/2.1.28%2Bdfsg1-5ubuntu3.1) |
| `libseccomp2:amd64` | 2.5.5-1ubuntu3.1 | LGPL-2.1 | [libseccomp 2.5.5-1ubuntu3.1](https://launchpad.net/ubuntu/+source/libseccomp/2.5.5-1ubuntu3.1) |
| `libselinux1:amd64` | 3.5-2ubuntu2.1 | GPL-2, public-domain | [libselinux 3.5-2ubuntu2.1](https://launchpad.net/ubuntu/+source/libselinux/3.5-2ubuntu2.1) |
| `libsemanage-common` | 3.5-1build5 | GPL-2, LGPL-2.1+ | [libsemanage 3.5-1build5](https://launchpad.net/ubuntu/+source/libsemanage/3.5-1build5) |
| `libsemanage2:amd64` | 3.5-1build5 | GPL-2, LGPL-2.1+ | [libsemanage 3.5-1build5](https://launchpad.net/ubuntu/+source/libsemanage/3.5-1build5) |
| `libsepol2:amd64` | 3.5-2build1 | GPL-2, GPL-2+, LGPL-2.1+, Zlib | [libsepol 3.5-2build1](https://launchpad.net/ubuntu/+source/libsepol/3.5-2build1) |
| `libsmartcols1:amd64` | 2.39.3-9ubuntu6.6 | BSD-3-clause, BSD-4-clause, BSLA, GPL-2, GPL-2+, GPL-3+, LGPL, LGPL-2+, LGPL-2.1+, LGPL-3+, MIT, public-domain | [util-linux 2.39.3-9ubuntu6.6](https://launchpad.net/ubuntu/+source/util-linux/2.39.3-9ubuntu6.6) |
| `libss2:amd64` | 1.47.0-2.4~exp1ubuntu4.1 | Apache-2, BSD-3-Clause, GPL or MIT-US-export, GPL-2, GPL-2+ with Texinfo exception, ISC, Kazlib, LGPL-2, Latex2e | [e2fsprogs 1.47.0-2.4~exp1ubuntu4.1](https://launchpad.net/ubuntu/+source/e2fsprogs/1.47.0-2.4~exp1ubuntu4.1) |
| `libssh-4:amd64` | 0.10.6-2ubuntu0.6 | BSD-2-clause, BSD-3-clause, LGPL-2.1, LGPL-2.1+~OpenSSL, LGPL-2.1+~OpenSSL or BSD-2-clause or BSD-3-clause, public-domain | [libssh 0.10.6-2ubuntu0.6](https://launchpad.net/ubuntu/+source/libssh/0.10.6-2ubuntu0.6) |
| `libssl3t64:amd64` | 3.0.13-0ubuntu3.16 | Apache-2.0, Artistic, Artistic or GPL-1+, GPL-1+ | [openssl 3.0.13-0ubuntu3.16](https://launchpad.net/ubuntu/+source/openssl/3.0.13-0ubuntu3.16) |
| `libstdc++6:amd64` | 14.2.0-4ubuntu2~24.04.1 | GPL-3.0-or-later with GCC Runtime Library Exception 3.1, additional notices | [gcc-14 14.2.0-4ubuntu2~24.04.1](https://launchpad.net/ubuntu/+source/gcc-14/14.2.0-4ubuntu2~24.04.1) |
| `libsystemd0:amd64` | 255.4-1ubuntu8.19 | CC0-1.0, Expat, GPL-2 with Linux-syscall-note exception, GPL-2+, LGPL-2.1+, public-domain | [systemd 255.4-1ubuntu8.19](https://launchpad.net/ubuntu/+source/systemd/255.4-1ubuntu8.19) |
| `libtasn1-6:amd64` | 4.19.0-3ubuntu0.24.04.2 | LGPL-2.1-or-later library, GPL-3.0-or-later utility and documentation terms | [libtasn1-6 4.19.0-3ubuntu0.24.04.2](https://launchpad.net/ubuntu/+source/libtasn1-6/4.19.0-3ubuntu0.24.04.2) |
| `libtinfo6:amd64` | 6.4+20240113-1ubuntu2.2 | BSD-3-clause, MIT/X11, X11 | [ncurses 6.4+20240113-1ubuntu2.2](https://launchpad.net/ubuntu/+source/ncurses/6.4%2B20240113-1ubuntu2.2) |
| `libudev1:amd64` | 255.4-1ubuntu8.19 | CC0-1.0, Expat, GPL-2 with Linux-syscall-note exception, GPL-2+, LGPL-2.1+, public-domain | [systemd 255.4-1ubuntu8.19](https://launchpad.net/ubuntu/+source/systemd/255.4-1ubuntu8.19) |
| `libunistring5:amd64` | 1.1-2build1.1 | FreeSoftware, GFDL-NIV-1.2+, GPL-2+, GPL-2+ with distribution exception, GPL-2+ with distribution exception, Expat and GPL-2+, GPL-3+, GPL-3+ or GFDL-NIV-1.2+, LGPL-3+, LGPL-3+ or GPL-2+, X11 | [libunistring 1.1-2build1.1](https://launchpad.net/ubuntu/+source/libunistring/1.1-2build1.1) |
| `libuuid1:amd64` | 2.39.3-9ubuntu6.6 | BSD-3-clause, BSD-4-clause, BSLA, GPL-2, GPL-2+, GPL-3+, LGPL, LGPL-2+, LGPL-2.1+, LGPL-3+, MIT, public-domain | [util-linux 2.39.3-9ubuntu6.6](https://launchpad.net/ubuntu/+source/util-linux/2.39.3-9ubuntu6.6) |
| `libxxhash0:amd64` | 0.8.2-2build1 | BSD-2-clause, BSD-2-clause or GPL-2+, GPL-2+ | [xxhash 0.8.2-2build1](https://launchpad.net/ubuntu/+source/xxhash/0.8.2-2build1) |
| `libzstd1:amd64` | 1.5.5+dfsg2-2build1.1 | BSD-3-clause, BSD-3-clause or GPL-2, Expat, GPL-2, zlib | [libzstd 1.5.5+dfsg2-2build1.1](https://launchpad.net/ubuntu/+source/libzstd/1.5.5%2Bdfsg2-2build1.1) |
| `login` | 1:4.13+dfsg1-4ubuntu3.2 | BSD-3-clause, GPL-1, GPL-2+, public-domain | [shadow 1:4.13+dfsg1-4ubuntu3.2](https://launchpad.net/ubuntu/+source/shadow/1%3A4.13%2Bdfsg1-4ubuntu3.2) |
| `logsave` | 1.47.0-2.4~exp1ubuntu4.1 | Apache-2, BSD-3-Clause, GPL or MIT-US-export, GPL-2, GPL-2+ with Texinfo exception, ISC, Kazlib, LGPL-2, Latex2e | [e2fsprogs 1.47.0-2.4~exp1ubuntu4.1](https://launchpad.net/ubuntu/+source/e2fsprogs/1.47.0-2.4~exp1ubuntu4.1) |
| `mawk` | 1.3.4.20240123-1build1 | CC-BY-3.0, GPL-2.0-only, X11 | [mawk 1.3.4.20240123-1build1](https://launchpad.net/ubuntu/+source/mawk/1.3.4.20240123-1build1) |
| `mount` | 2.39.3-9ubuntu6.6 | BSD-3-clause, BSD-4-clause, BSLA, GPL-2, GPL-2+, GPL-3+, LGPL, LGPL-2+, LGPL-2.1+, LGPL-3+, MIT, public-domain | [util-linux 2.39.3-9ubuntu6.6](https://launchpad.net/ubuntu/+source/util-linux/2.39.3-9ubuntu6.6) |
| `ncurses-base` | 6.4+20240113-1ubuntu2.2 | BSD-3-clause, MIT/X11, X11 | [ncurses 6.4+20240113-1ubuntu2.2](https://launchpad.net/ubuntu/+source/ncurses/6.4%2B20240113-1ubuntu2.2) |
| `ncurses-bin` | 6.4+20240113-1ubuntu2.2 | BSD-3-clause, MIT/X11, X11 | [ncurses 6.4+20240113-1ubuntu2.2](https://launchpad.net/ubuntu/+source/ncurses/6.4%2B20240113-1ubuntu2.2) |
| `openssl` | 3.0.13-0ubuntu3.16 | Apache-2.0, Artistic, Artistic or GPL-1+, GPL-1+ | [openssl 3.0.13-0ubuntu3.16](https://launchpad.net/ubuntu/+source/openssl/3.0.13-0ubuntu3.16) |
| `passwd` | 1:4.13+dfsg1-4ubuntu3.2 | BSD-3-clause, GPL-1, GPL-2+, public-domain | [shadow 1:4.13+dfsg1-4ubuntu3.2](https://launchpad.net/ubuntu/+source/shadow/1%3A4.13%2Bdfsg1-4ubuntu3.2) |
| `perl-base` | 5.38.2-3.2ubuntu0.7 | Artistic, Artistic or GPL-1+ or Artistic-dist, Artistic-2, Artistic-dist, BSD-3-clause, BSD-3-clause-GENERIC, BSD-3-clause-with-weird-numbering, BSD-4-clause-POWERDOG, BZIP, CC0-1.0, DONT-CHANGE-THE-GPL, Expat, Expat or GPL-1+ or Artistic, FSFAP, GPL-1+, GPL-1+ or Artistic, GPL-1+ or Artistic or Artistic-dist, GPL-1+ or Artistic, and BSD-3-clause-GENERIC, GPL-1+ or Artistic, and BSD-4-clause-POWERDOG, GPL-1+ or Artistic, and Expat, GPL-1+ or Artistic, and Unicode, GPL-2+, GPL-2+ or Artistic, GPL-3+-WITH-BISON-EXCEPTION, LGPL-2.1, REGCOMP, REGCOMP, and GPL-1+ or Artistic, SDBM-PUBLIC-DOMAIN, TEXT-TABS, Unicode, ZLIB | [perl 5.38.2-3.2ubuntu0.7](https://launchpad.net/ubuntu/+source/perl/5.38.2-3.2ubuntu0.7) |
| `procps` | 2:4.0.4-4ubuntu3.3 | GPL-2.0+, LGPL-2.0+, LGPL-2.1+ | [procps 2:4.0.4-4ubuntu3.3](https://launchpad.net/ubuntu/+source/procps/2%3A4.0.4-4ubuntu3.3) |
| `sed` | 4.9-2ubuntu0.24.04.1 | BSD-4-clause-UC, BSL-1, GFDL-NIV-1.3+, GPL-3+, GPL-3+ and ISC, ISC, X11, pcre | [sed 4.9-2ubuntu0.24.04.1](https://launchpad.net/ubuntu/+source/sed/4.9-2ubuntu0.24.04.1) |
| `sensible-utils` | 0.0.22 | All-permissive, BSD-2-clause, GPL-2+, configure, installsh | [sensible-utils 0.0.22](https://launchpad.net/ubuntu/+source/sensible-utils/0.0.22) |
| `sysvinit-utils` | 3.08-6ubuntu3 | GPL-2.0, GPL-2.0+, GPL-3.0, LGPL-2.1+ | [sysvinit 3.08-6ubuntu3](https://launchpad.net/ubuntu/+source/sysvinit/3.08-6ubuntu3) |
| `tar` | 1.35+dfsg-3ubuntu0.4 | GPL-2+, GPL-3+, GPL-3+ with Bison exception, LGPL-2.1+, LGPL-3+ | [tar 1.35+dfsg-3ubuntu0.4](https://launchpad.net/ubuntu/+source/tar/1.35%2Bdfsg-3ubuntu0.4) |
| `tzdata` | 2026f-0ubuntu0.24.04.1 | ICU, public-domain | [tzdata 2026f-0ubuntu0.24.04.1](https://launchpad.net/ubuntu/+source/tzdata/2026f-0ubuntu0.24.04.1) |
| `tzdata-legacy` | 2026f-0ubuntu0.24.04.1 | ICU, public-domain | [tzdata 2026f-0ubuntu0.24.04.1](https://launchpad.net/ubuntu/+source/tzdata/2026f-0ubuntu0.24.04.1) |
| `ubuntu-keyring` | 2023.11.28.1 | GPL-2.0-or-later | [ubuntu-keyring 2023.11.28.1](https://launchpad.net/ubuntu/+source/ubuntu-keyring/2023.11.28.1) |
| `unminimize` | 0.2.1 | GPL-2.0+ | [unminimize 0.2.1](https://launchpad.net/ubuntu/+source/unminimize/0.2.1) |
| `util-linux` | 2.39.3-9ubuntu6.6 | BSD-3-clause, BSD-4-clause, BSLA, GPL-2, GPL-2+, GPL-3+, LGPL, LGPL-2+, LGPL-2.1+, LGPL-3+, MIT, public-domain | [util-linux 2.39.3-9ubuntu6.6](https://launchpad.net/ubuntu/+source/util-linux/2.39.3-9ubuntu6.6) |
| `zlib1g:amd64` | 1:1.3.dfsg-3.1ubuntu2.3 | Zlib | [zlib 1:1.3.dfsg-3.1ubuntu2.3](https://launchpad.net/ubuntu/+source/zlib/1%3A1.3.dfsg-3.1ubuntu2.3) |

## Shipped assets and attribution

- Vite publishes the repository `FrontEnd/public` tree, the application and `promo.html`. Guide captures, flags, splash/icon artwork, legal assets, promo artwork and spreadsheet templates need recorded provenance and rights for the exact shipped files. The guide and promo capture scripts use synthetic local data. Browser UI appearing in tutorial captures and any third-party artwork are separate provenance items. Repository ownership of original assets and authorization for third-party elements were not independently established.
- The Guide identifies five supplied Safari captures (`safari-*.webp`) and five supplied Android Chrome captures (`android-*.webp`) under `FrontEnd/public/guide/getting-started/`. Google's published allowance for standard teaching materials describes unaltered captures. Apple publishes separate rules for its trademarks and imagery in instructional material. Clearance for the exact supplied crops and their distribution in the paid SaaS guide remains unverified. [Guide source record](../FrontEnd/public/guide/README.md) · [Apple third-party guidelines](https://www.apple.com/legal/intellectual-property/guidelinesfor3rdparties.html) · [Google brand guidance](https://about.google/brand-resource-center/guidance/) · [Google brand terms](https://about.google/intl/en/brand-resource-center/brand-terms/)
- The release GIFs under `release-assets/2026-11-23/gifs` are separate publicity assets. Verify their provenance before publishing them. The current Dockerfile copies Vite output and the published API. It does not copy the release-assets directory.
- Retain original copyright, permission and disclaimer text for MIT/BSD/ISC-family components. Include Apache-2.0 terms and any upstream NOTICE, and the creator attribution, license link and modification indication for any distributed CC-BY-4.0 item. Preserve the OFL and complete native notices included in the publish output. The exact final browser-bundle and container notice set remains a release gate.
- Browser and driver binaries downloaded by Playwright are test tooling. Their vendor notices remain with their installations and are not part of the application image. Exact engine binaries and platform-specific terms are not covered by the npm package license rows.

## Resolved NuGet packages

Version and license evidence comes from the SDK restore graph and exact local `.nuspec` files, with a link to each exact NuGet package. `WebApi`, `BusinessLogicLayer`, and `DataAccessLayer` form the application project graph. `GF3.Tests` also includes test-only packages. `GF3.Launcher` has no PackageReference and the stress harness inherits BusinessLogicLayer and DataAccessLayer. SDK, framework packs, and OS packages are outside these rows.

83 distinct versions.

| Package | Version | License | Resolved scope | Source |
| --- | --- | --- | --- | --- |
| `ClosedXML` | 0.105.0 | MIT | WebApi transitive, BusinessLogicLayer direct, GF3.Tests direct | [NuGet](https://www.nuget.org/packages/ClosedXML/0.105.0) |
| `ClosedXML.Parser` | 2.0.0 | MIT | WebApi transitive, BusinessLogicLayer transitive, GF3.Tests transitive | [NuGet](https://www.nuget.org/packages/ClosedXML.Parser/2.0.0) |
| `coverlet.collector` | 6.0.4 | MIT | GF3.Tests direct | [NuGet](https://www.nuget.org/packages/coverlet.collector/6.0.4) |
| `DocumentFormat.OpenXml` | 3.1.1 | MIT | WebApi transitive, BusinessLogicLayer transitive, GF3.Tests transitive | [NuGet](https://www.nuget.org/packages/DocumentFormat.OpenXml/3.1.1) |
| `DocumentFormat.OpenXml.Framework` | 3.1.1 | MIT | WebApi transitive, BusinessLogicLayer transitive, GF3.Tests transitive | [NuGet](https://www.nuget.org/packages/DocumentFormat.OpenXml.Framework/3.1.1) |
| `ExcelNumberFormat` | 1.1.0 | MIT | WebApi transitive, BusinessLogicLayer transitive, GF3.Tests transitive | [NuGet](https://www.nuget.org/packages/ExcelNumberFormat/1.1.0) |
| `Humanizer.Core` | 2.14.1 | MIT | WebApi transitive, DataAccessLayer transitive | [NuGet](https://www.nuget.org/packages/Humanizer.Core/2.14.1) |
| `Lib.Net.Http.EncryptedContentEncoding` | 2.1.1 | MIT | WebApi transitive, GF3.Tests transitive | [NuGet](https://www.nuget.org/packages/Lib.Net.Http.EncryptedContentEncoding/2.1.1) |
| `Lib.Net.Http.WebPush` | 3.3.1 | MIT | WebApi direct, GF3.Tests transitive | [NuGet](https://www.nuget.org/packages/Lib.Net.Http.WebPush/3.3.1) |
| `Microsoft.AspNetCore.OpenApi` | 10.0.9 | MIT | WebApi direct, GF3.Tests transitive | [NuGet](https://www.nuget.org/packages/Microsoft.AspNetCore.OpenApi/10.0.9) |
| `Microsoft.AspNetCore.SpaProxy` | 10.0.9 | MIT | WebApi direct, GF3.Tests transitive | [NuGet](https://www.nuget.org/packages/Microsoft.AspNetCore.SpaProxy/10.0.9) |
| `Microsoft.AspNetCore.SpaServices.Extensions` | 10.0.9 | MIT | WebApi direct, GF3.Tests transitive | [NuGet](https://www.nuget.org/packages/Microsoft.AspNetCore.SpaServices.Extensions/10.0.9) |
| `Microsoft.Build.Framework` | 18.0.2 | MIT | WebApi transitive, DataAccessLayer transitive | [NuGet](https://www.nuget.org/packages/Microsoft.Build.Framework/18.0.2) |
| `Microsoft.CodeAnalysis.Analyzers` | 3.11.0 | MIT | WebApi transitive, DataAccessLayer transitive | [NuGet](https://www.nuget.org/packages/Microsoft.CodeAnalysis.Analyzers/3.11.0) |
| `Microsoft.CodeAnalysis.Common` | 5.0.0 | MIT | WebApi transitive, DataAccessLayer transitive | [NuGet](https://www.nuget.org/packages/Microsoft.CodeAnalysis.Common/5.0.0) |
| `Microsoft.CodeAnalysis.CSharp` | 5.0.0 | MIT | WebApi transitive, DataAccessLayer transitive | [NuGet](https://www.nuget.org/packages/Microsoft.CodeAnalysis.CSharp/5.0.0) |
| `Microsoft.CodeAnalysis.CSharp.Workspaces` | 5.0.0 | MIT | WebApi transitive, DataAccessLayer transitive | [NuGet](https://www.nuget.org/packages/Microsoft.CodeAnalysis.CSharp.Workspaces/5.0.0) |
| `Microsoft.CodeAnalysis.Workspaces.Common` | 5.0.0 | MIT | WebApi transitive, DataAccessLayer transitive | [NuGet](https://www.nuget.org/packages/Microsoft.CodeAnalysis.Workspaces.Common/5.0.0) |
| `Microsoft.CodeAnalysis.Workspaces.MSBuild` | 5.0.0 | MIT | WebApi transitive, DataAccessLayer transitive | [NuGet](https://www.nuget.org/packages/Microsoft.CodeAnalysis.Workspaces.MSBuild/5.0.0) |
| `Microsoft.CodeCoverage` | 17.14.1 | MIT | GF3.Tests transitive | [NuGet](https://www.nuget.org/packages/Microsoft.CodeCoverage/17.14.1) |
| `Microsoft.Data.Sqlite.Core` | 10.0.13 | MIT | WebApi transitive, BusinessLogicLayer transitive, DataAccessLayer direct, GF3.Tests transitive | [NuGet](https://www.nuget.org/packages/Microsoft.Data.Sqlite.Core/10.0.13) |
| `Microsoft.EntityFrameworkCore` | 10.0.9 | MIT | WebApi transitive, BusinessLogicLayer transitive, DataAccessLayer direct, GF3.Tests direct | [NuGet](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore/10.0.9) |
| `Microsoft.EntityFrameworkCore.Abstractions` | 10.0.9 | MIT | WebApi transitive, BusinessLogicLayer transitive, DataAccessLayer transitive, GF3.Tests transitive | [NuGet](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.Abstractions/10.0.9) |
| `Microsoft.EntityFrameworkCore.Analyzers` | 10.0.9 | MIT | WebApi transitive, BusinessLogicLayer transitive, DataAccessLayer transitive, GF3.Tests transitive | [NuGet](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.Analyzers/10.0.9) |
| `Microsoft.EntityFrameworkCore.Design` | 10.0.9 | MIT | WebApi direct, DataAccessLayer direct | [NuGet](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.Design/10.0.9) |
| `Microsoft.EntityFrameworkCore.Relational` | 10.0.9 | MIT | WebApi transitive, BusinessLogicLayer transitive, DataAccessLayer transitive, GF3.Tests direct | [NuGet](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.Relational/10.0.9) |
| `Microsoft.EntityFrameworkCore.Sqlite` | 10.0.9 | MIT | WebApi transitive, BusinessLogicLayer transitive, DataAccessLayer direct, GF3.Tests direct | [NuGet](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.Sqlite/10.0.9) |
| `Microsoft.EntityFrameworkCore.Sqlite.Core` | 10.0.9 | MIT | WebApi transitive, BusinessLogicLayer transitive, DataAccessLayer transitive, GF3.Tests transitive | [NuGet](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.Sqlite.Core/10.0.9) |
| `Microsoft.EntityFrameworkCore.Tools` | 10.0.9 | MIT | DataAccessLayer direct | [NuGet](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.Tools/10.0.9) |
| `Microsoft.Extensions.ApiDescription.Server` | 10.0.0 | MIT | WebApi transitive, GF3.Tests transitive | [NuGet](https://www.nuget.org/packages/Microsoft.Extensions.ApiDescription.Server/10.0.0) |
| `Microsoft.Extensions.Caching.Abstractions` | 10.0.9 | MIT | BusinessLogicLayer transitive, DataAccessLayer transitive | [NuGet](https://www.nuget.org/packages/Microsoft.Extensions.Caching.Abstractions/10.0.9) |
| `Microsoft.Extensions.Caching.Memory` | 10.0.9 | MIT | BusinessLogicLayer transitive, DataAccessLayer transitive | [NuGet](https://www.nuget.org/packages/Microsoft.Extensions.Caching.Memory/10.0.9) |
| `Microsoft.Extensions.Configuration` | 10.0.0 | MIT | BusinessLogicLayer transitive | [NuGet](https://www.nuget.org/packages/Microsoft.Extensions.Configuration/10.0.0) |
| `Microsoft.Extensions.Configuration.Abstractions` | 10.0.9 | MIT | BusinessLogicLayer transitive, DataAccessLayer transitive | [NuGet](https://www.nuget.org/packages/Microsoft.Extensions.Configuration.Abstractions/10.0.9) |
| `Microsoft.Extensions.Configuration.Binder` | 10.0.0 | MIT | BusinessLogicLayer transitive | [NuGet](https://www.nuget.org/packages/Microsoft.Extensions.Configuration.Binder/10.0.0) |
| `Microsoft.Extensions.DependencyInjection` | 10.0.9 | MIT | BusinessLogicLayer transitive, DataAccessLayer transitive | [NuGet](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection/10.0.9) |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | 10.0.9 | MIT | BusinessLogicLayer transitive, DataAccessLayer transitive | [NuGet](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection.Abstractions/10.0.9) |
| `Microsoft.Extensions.DependencyModel` | 10.0.9 | MIT | WebApi transitive, BusinessLogicLayer transitive, DataAccessLayer transitive, GF3.Tests transitive | [NuGet](https://www.nuget.org/packages/Microsoft.Extensions.DependencyModel/10.0.9) |
| `Microsoft.Extensions.Logging` | 10.0.9 | MIT | BusinessLogicLayer transitive, DataAccessLayer transitive | [NuGet](https://www.nuget.org/packages/Microsoft.Extensions.Logging/10.0.9) |
| `Microsoft.Extensions.Logging.Abstractions` | 10.0.9 | MIT | BusinessLogicLayer transitive, DataAccessLayer transitive | [NuGet](https://www.nuget.org/packages/Microsoft.Extensions.Logging.Abstractions/10.0.9) |
| `Microsoft.Extensions.Options` | 10.0.9 | MIT | BusinessLogicLayer transitive, DataAccessLayer transitive | [NuGet](https://www.nuget.org/packages/Microsoft.Extensions.Options/10.0.9) |
| `Microsoft.Extensions.Options.ConfigurationExtensions` | 10.0.0 | MIT | BusinessLogicLayer direct | [NuGet](https://www.nuget.org/packages/Microsoft.Extensions.Options.ConfigurationExtensions/10.0.0) |
| `Microsoft.Extensions.Primitives` | 10.0.9 | MIT | BusinessLogicLayer transitive, DataAccessLayer transitive | [NuGet](https://www.nuget.org/packages/Microsoft.Extensions.Primitives/10.0.9) |
| `Microsoft.NET.Test.Sdk` | 17.14.1 | MIT | GF3.Tests direct | [NuGet](https://www.nuget.org/packages/Microsoft.NET.Test.Sdk/17.14.1) |
| `Microsoft.OpenApi` | 2.7.5 | MIT | WebApi transitive, GF3.Tests transitive | [NuGet](https://www.nuget.org/packages/Microsoft.OpenApi/2.7.5) |
| `Microsoft.TestPlatform.ObjectModel` | 17.14.1 | MIT | GF3.Tests transitive | [NuGet](https://www.nuget.org/packages/Microsoft.TestPlatform.ObjectModel/17.14.1) |
| `Microsoft.TestPlatform.TestHost` | 17.14.1 | MIT | GF3.Tests transitive | [NuGet](https://www.nuget.org/packages/Microsoft.TestPlatform.TestHost/17.14.1) |
| `Microsoft.VisualStudio.SolutionPersistence` | 1.0.52 | MIT | WebApi transitive, DataAccessLayer transitive | [NuGet](https://www.nuget.org/packages/Microsoft.VisualStudio.SolutionPersistence/1.0.52) |
| `Mono.TextTemplating` | 3.0.0 | MIT | WebApi transitive, DataAccessLayer transitive | [NuGet](https://www.nuget.org/packages/Mono.TextTemplating/3.0.0) |
| `Newtonsoft.Json` | 13.0.3 | MIT | WebApi transitive, DataAccessLayer transitive, GF3.Tests transitive | [NuGet](https://www.nuget.org/packages/Newtonsoft.Json/13.0.3) |
| `PDFsharp` | 6.2.4 | MIT | WebApi direct, GF3.Tests transitive | [NuGet](https://www.nuget.org/packages/PDFsharp/6.2.4) |
| `RBush.Signed` | 4.0.0 | MIT | WebApi transitive, BusinessLogicLayer transitive, GF3.Tests transitive | [NuGet](https://www.nuget.org/packages/RBush.Signed/4.0.0) |
| `SixLabors.Fonts` | 1.0.0 | Apache-2.0 | WebApi transitive, BusinessLogicLayer transitive, GF3.Tests transitive | [NuGet](https://www.nuget.org/packages/SixLabors.Fonts/1.0.0) |
| `SkiaSharp` | 4.157.0 | MIT | WebApi direct, GF3.Tests transitive | [NuGet](https://www.nuget.org/packages/SkiaSharp/4.157.0) |
| `SkiaSharp.NativeAssets.Linux.NoDependencies` | 4.157.0 | MIT | WebApi direct, GF3.Tests transitive | [NuGet](https://www.nuget.org/packages/SkiaSharp.NativeAssets.Linux.NoDependencies/4.157.0) |
| `SkiaSharp.NativeAssets.macOS` | 4.157.0 | MIT | WebApi transitive, GF3.Tests transitive | [NuGet](https://www.nuget.org/packages/SkiaSharp.NativeAssets.macOS/4.157.0) |
| `SkiaSharp.NativeAssets.Win32` | 4.157.0 | MIT | WebApi transitive, GF3.Tests transitive | [NuGet](https://www.nuget.org/packages/SkiaSharp.NativeAssets.Win32/4.157.0) |
| `SourceGear.sqlite3` | 3.50.4.5 | Public domain (package LICENSE.txt) | WebApi transitive, BusinessLogicLayer transitive, DataAccessLayer transitive, GF3.Tests transitive | [NuGet](https://www.nuget.org/packages/SourceGear.sqlite3/3.50.4.5) |
| `SQLitePCLRaw.bundle_e_sqlite3` | 3.0.3 | Apache-2.0 | WebApi transitive, BusinessLogicLayer transitive, DataAccessLayer direct, GF3.Tests transitive | [NuGet](https://www.nuget.org/packages/SQLitePCLRaw.bundle_e_sqlite3/3.0.3) |
| `SQLitePCLRaw.config.e_sqlite3` | 3.0.3 | Apache-2.0 | WebApi transitive, BusinessLogicLayer transitive, DataAccessLayer transitive, GF3.Tests transitive | [NuGet](https://www.nuget.org/packages/SQLitePCLRaw.config.e_sqlite3/3.0.3) |
| `SQLitePCLRaw.core` | 3.0.3 | Apache-2.0 | WebApi transitive, BusinessLogicLayer transitive, DataAccessLayer transitive, GF3.Tests transitive | [NuGet](https://www.nuget.org/packages/SQLitePCLRaw.core/3.0.3) |
| `SQLitePCLRaw.provider.e_sqlite3` | 3.0.3 | Apache-2.0 | WebApi transitive, BusinessLogicLayer transitive, DataAccessLayer transitive, GF3.Tests transitive | [NuGet](https://www.nuget.org/packages/SQLitePCLRaw.provider.e_sqlite3/3.0.3) |
| `Swashbuckle.AspNetCore` | 10.2.3 | MIT | WebApi direct, GF3.Tests transitive | [NuGet](https://www.nuget.org/packages/Swashbuckle.AspNetCore/10.2.3) |
| `Swashbuckle.AspNetCore.Swagger` | 10.2.3 | MIT | WebApi transitive, GF3.Tests transitive | [NuGet](https://www.nuget.org/packages/Swashbuckle.AspNetCore.Swagger/10.2.3) |
| `Swashbuckle.AspNetCore.SwaggerGen` | 10.2.3 | MIT | WebApi transitive, GF3.Tests transitive | [NuGet](https://www.nuget.org/packages/Swashbuckle.AspNetCore.SwaggerGen/10.2.3) |
| `Swashbuckle.AspNetCore.SwaggerUI` | 10.2.3 | MIT | WebApi transitive, GF3.Tests transitive | [NuGet](https://www.nuget.org/packages/Swashbuckle.AspNetCore.SwaggerUI/10.2.3) |
| `System.CodeDom` | 6.0.0 | MIT | WebApi transitive, DataAccessLayer transitive | [NuGet](https://www.nuget.org/packages/System.CodeDom/6.0.0) |
| `System.Composition` | 9.0.0 | MIT | WebApi transitive, DataAccessLayer transitive | [NuGet](https://www.nuget.org/packages/System.Composition/9.0.0) |
| `System.Composition.AttributedModel` | 9.0.0 | MIT | WebApi transitive, DataAccessLayer transitive | [NuGet](https://www.nuget.org/packages/System.Composition.AttributedModel/9.0.0) |
| `System.Composition.Convention` | 9.0.0 | MIT | WebApi transitive, DataAccessLayer transitive | [NuGet](https://www.nuget.org/packages/System.Composition.Convention/9.0.0) |
| `System.Composition.Hosting` | 9.0.0 | MIT | WebApi transitive, DataAccessLayer transitive | [NuGet](https://www.nuget.org/packages/System.Composition.Hosting/9.0.0) |
| `System.Composition.Runtime` | 9.0.0 | MIT | WebApi transitive, DataAccessLayer transitive | [NuGet](https://www.nuget.org/packages/System.Composition.Runtime/9.0.0) |
| `System.Composition.TypedParts` | 9.0.0 | MIT | WebApi transitive, DataAccessLayer transitive | [NuGet](https://www.nuget.org/packages/System.Composition.TypedParts/9.0.0) |
| `System.IO.Packaging` | 8.0.1 | MIT | WebApi transitive, BusinessLogicLayer transitive, GF3.Tests transitive | [NuGet](https://www.nuget.org/packages/System.IO.Packaging/8.0.1) |
| `System.Security.Cryptography.Pkcs` | 8.0.2 | MIT | WebApi direct, GF3.Tests transitive | [NuGet](https://www.nuget.org/packages/System.Security.Cryptography.Pkcs/8.0.2) |
| `xunit` | 2.9.3 | Apache-2.0 | GF3.Tests direct | [NuGet](https://www.nuget.org/packages/xunit/2.9.3) |
| `xunit.abstractions` | 2.0.3 | Apache-2.0 (upstream license; legacy nuspec URL) | GF3.Tests transitive | [NuGet](https://www.nuget.org/packages/xunit.abstractions/2.0.3) |
| `xunit.analyzers` | 1.18.0 | Apache-2.0 | GF3.Tests transitive | [NuGet](https://www.nuget.org/packages/xunit.analyzers/1.18.0) |
| `xunit.assert` | 2.9.3 | Apache-2.0 | GF3.Tests transitive | [NuGet](https://www.nuget.org/packages/xunit.assert/2.9.3) |
| `xunit.core` | 2.9.3 | Apache-2.0 | GF3.Tests transitive | [NuGet](https://www.nuget.org/packages/xunit.core/2.9.3) |
| `xunit.extensibility.core` | 2.9.3 | Apache-2.0 | GF3.Tests transitive | [NuGet](https://www.nuget.org/packages/xunit.extensibility.core/2.9.3) |
| `xunit.extensibility.execution` | 2.9.3 | Apache-2.0 | GF3.Tests transitive | [NuGet](https://www.nuget.org/packages/xunit.extensibility.execution/2.9.3) |
| `xunit.runner.visualstudio` | 3.1.4 | Apache-2.0 | GF3.Tests direct | [NuGet](https://www.nuget.org/packages/xunit.runner.visualstudio/3.1.4) |

## Resolved npm packages

385 locked package entries, including optional platform variants. Scope and declared license come from `FrontEnd/package-lock.json`; the links identify the corresponding exact registry records. Runtime entries are eligible dependencies of the application. Inspect the built bundle to establish what is actually shipped.

| Package | Version | Declared license | Scope | Source |
| --- | --- | --- | --- | --- |
| `@adobe/css-tools` | 4.4.4 | MIT | development transitive | [Registry](https://registry.npmjs.org/@adobe/css-tools/4.4.4) |
| `@asamuzakjp/css-color` | 5.1.11 | MIT | development transitive | [Registry](https://registry.npmjs.org/@asamuzakjp/css-color/5.1.11) |
| `@asamuzakjp/dom-selector` | 7.1.1 | MIT | development transitive | [Registry](https://registry.npmjs.org/@asamuzakjp/dom-selector/7.1.1) |
| `@asamuzakjp/generational-cache` | 1.0.1 | MIT | development transitive | [Registry](https://registry.npmjs.org/@asamuzakjp/generational-cache/1.0.1) |
| `@asamuzakjp/nwsapi` | 2.3.9 | MIT | development transitive | [Registry](https://registry.npmjs.org/@asamuzakjp/nwsapi/2.3.9) |
| `@babel/code-frame` | 7.29.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/@babel/code-frame/7.29.0) |
| `@babel/compat-data` | 7.29.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/@babel/compat-data/7.29.0) |
| `@babel/core` | 7.29.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/@babel/core/7.29.0) |
| `@babel/generator` | 7.29.1 | MIT | development transitive | [Registry](https://registry.npmjs.org/@babel/generator/7.29.1) |
| `@babel/helper-compilation-targets` | 7.28.6 | MIT | development transitive | [Registry](https://registry.npmjs.org/@babel/helper-compilation-targets/7.28.6) |
| `@babel/helper-globals` | 7.28.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/@babel/helper-globals/7.28.0) |
| `@babel/helper-module-imports` | 7.28.6 | MIT | development transitive | [Registry](https://registry.npmjs.org/@babel/helper-module-imports/7.28.6) |
| `@babel/helper-module-transforms` | 7.28.6 | MIT | development transitive | [Registry](https://registry.npmjs.org/@babel/helper-module-transforms/7.28.6) |
| `@babel/helper-plugin-utils` | 7.28.6 | MIT | development transitive | [Registry](https://registry.npmjs.org/@babel/helper-plugin-utils/7.28.6) |
| `@babel/helper-string-parser` | 7.27.1 | MIT | development transitive | [Registry](https://registry.npmjs.org/@babel/helper-string-parser/7.27.1) |
| `@babel/helper-validator-identifier` | 7.28.5 | MIT | development transitive | [Registry](https://registry.npmjs.org/@babel/helper-validator-identifier/7.28.5) |
| `@babel/helper-validator-option` | 7.27.1 | MIT | development transitive | [Registry](https://registry.npmjs.org/@babel/helper-validator-option/7.27.1) |
| `@babel/helpers` | 7.28.6 | MIT | development transitive | [Registry](https://registry.npmjs.org/@babel/helpers/7.28.6) |
| `@babel/parser` | 7.29.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/@babel/parser/7.29.0) |
| `@babel/plugin-transform-react-jsx-self` | 7.27.1 | MIT | development transitive | [Registry](https://registry.npmjs.org/@babel/plugin-transform-react-jsx-self/7.27.1) |
| `@babel/plugin-transform-react-jsx-source` | 7.27.1 | MIT | development transitive | [Registry](https://registry.npmjs.org/@babel/plugin-transform-react-jsx-source/7.27.1) |
| `@babel/runtime` | 7.29.2 | MIT | development transitive | [Registry](https://registry.npmjs.org/@babel/runtime/7.29.2) |
| `@babel/template` | 7.28.6 | MIT | development transitive | [Registry](https://registry.npmjs.org/@babel/template/7.28.6) |
| `@babel/traverse` | 7.29.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/@babel/traverse/7.29.0) |
| `@babel/types` | 7.29.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/@babel/types/7.29.0) |
| `@bramus/specificity` | 2.4.2 | MIT | development transitive | [Registry](https://registry.npmjs.org/@bramus/specificity/2.4.2) |
| `@csstools/color-helpers` | 6.0.2 | MIT-0 | development transitive | [Registry](https://registry.npmjs.org/@csstools/color-helpers/6.0.2) |
| `@csstools/css-calc` | 3.2.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/@csstools/css-calc/3.2.0) |
| `@csstools/css-color-parser` | 4.1.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/@csstools/css-color-parser/4.1.0) |
| `@csstools/css-parser-algorithms` | 4.0.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/@csstools/css-parser-algorithms/4.0.0) |
| `@csstools/css-syntax-patches-for-csstree` | 1.1.3 | MIT-0 | development transitive | [Registry](https://registry.npmjs.org/@csstools/css-syntax-patches-for-csstree/1.1.3) |
| `@csstools/css-tokenizer` | 4.0.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/@csstools/css-tokenizer/4.0.0) |
| `@dnd-kit/accessibility` | 3.1.1 | MIT | runtime transitive | [Registry](https://registry.npmjs.org/@dnd-kit/accessibility/3.1.1) |
| `@dnd-kit/core` | 6.3.1 | MIT | runtime direct | [Registry](https://registry.npmjs.org/@dnd-kit/core/6.3.1) |
| `@dnd-kit/sortable` | 10.0.0 | MIT | runtime direct | [Registry](https://registry.npmjs.org/@dnd-kit/sortable/10.0.0) |
| `@dnd-kit/utilities` | 3.2.2 | MIT | runtime direct | [Registry](https://registry.npmjs.org/@dnd-kit/utilities/3.2.2) |
| `@esbuild/aix-ppc64` | 0.27.3 | MIT | development transitive, optional | [Registry](https://registry.npmjs.org/@esbuild/aix-ppc64/0.27.3) |
| `@esbuild/android-arm` | 0.27.3 | MIT | development transitive, optional | [Registry](https://registry.npmjs.org/@esbuild/android-arm/0.27.3) |
| `@esbuild/android-arm64` | 0.27.3 | MIT | development transitive, optional | [Registry](https://registry.npmjs.org/@esbuild/android-arm64/0.27.3) |
| `@esbuild/android-x64` | 0.27.3 | MIT | development transitive, optional | [Registry](https://registry.npmjs.org/@esbuild/android-x64/0.27.3) |
| `@esbuild/darwin-arm64` | 0.27.3 | MIT | development transitive, optional | [Registry](https://registry.npmjs.org/@esbuild/darwin-arm64/0.27.3) |
| `@esbuild/darwin-x64` | 0.27.3 | MIT | development transitive, optional | [Registry](https://registry.npmjs.org/@esbuild/darwin-x64/0.27.3) |
| `@esbuild/freebsd-arm64` | 0.27.3 | MIT | development transitive, optional | [Registry](https://registry.npmjs.org/@esbuild/freebsd-arm64/0.27.3) |
| `@esbuild/freebsd-x64` | 0.27.3 | MIT | development transitive, optional | [Registry](https://registry.npmjs.org/@esbuild/freebsd-x64/0.27.3) |
| `@esbuild/linux-arm` | 0.27.3 | MIT | development transitive, optional | [Registry](https://registry.npmjs.org/@esbuild/linux-arm/0.27.3) |
| `@esbuild/linux-arm64` | 0.27.3 | MIT | development transitive, optional | [Registry](https://registry.npmjs.org/@esbuild/linux-arm64/0.27.3) |
| `@esbuild/linux-ia32` | 0.27.3 | MIT | development transitive, optional | [Registry](https://registry.npmjs.org/@esbuild/linux-ia32/0.27.3) |
| `@esbuild/linux-loong64` | 0.27.3 | MIT | development transitive, optional | [Registry](https://registry.npmjs.org/@esbuild/linux-loong64/0.27.3) |
| `@esbuild/linux-mips64el` | 0.27.3 | MIT | development transitive, optional | [Registry](https://registry.npmjs.org/@esbuild/linux-mips64el/0.27.3) |
| `@esbuild/linux-ppc64` | 0.27.3 | MIT | development transitive, optional | [Registry](https://registry.npmjs.org/@esbuild/linux-ppc64/0.27.3) |
| `@esbuild/linux-riscv64` | 0.27.3 | MIT | development transitive, optional | [Registry](https://registry.npmjs.org/@esbuild/linux-riscv64/0.27.3) |
| `@esbuild/linux-s390x` | 0.27.3 | MIT | development transitive, optional | [Registry](https://registry.npmjs.org/@esbuild/linux-s390x/0.27.3) |
| `@esbuild/linux-x64` | 0.27.3 | MIT | development transitive, optional | [Registry](https://registry.npmjs.org/@esbuild/linux-x64/0.27.3) |
| `@esbuild/netbsd-arm64` | 0.27.3 | MIT | development transitive, optional | [Registry](https://registry.npmjs.org/@esbuild/netbsd-arm64/0.27.3) |
| `@esbuild/netbsd-x64` | 0.27.3 | MIT | development transitive, optional | [Registry](https://registry.npmjs.org/@esbuild/netbsd-x64/0.27.3) |
| `@esbuild/openbsd-arm64` | 0.27.3 | MIT | development transitive, optional | [Registry](https://registry.npmjs.org/@esbuild/openbsd-arm64/0.27.3) |
| `@esbuild/openbsd-x64` | 0.27.3 | MIT | development transitive, optional | [Registry](https://registry.npmjs.org/@esbuild/openbsd-x64/0.27.3) |
| `@esbuild/openharmony-arm64` | 0.27.3 | MIT | development transitive, optional | [Registry](https://registry.npmjs.org/@esbuild/openharmony-arm64/0.27.3) |
| `@esbuild/sunos-x64` | 0.27.3 | MIT | development transitive, optional | [Registry](https://registry.npmjs.org/@esbuild/sunos-x64/0.27.3) |
| `@esbuild/win32-arm64` | 0.27.3 | MIT | development transitive, optional | [Registry](https://registry.npmjs.org/@esbuild/win32-arm64/0.27.3) |
| `@esbuild/win32-ia32` | 0.27.3 | MIT | development transitive, optional | [Registry](https://registry.npmjs.org/@esbuild/win32-ia32/0.27.3) |
| `@esbuild/win32-x64` | 0.27.3 | MIT | development transitive, optional | [Registry](https://registry.npmjs.org/@esbuild/win32-x64/0.27.3) |
| `@eslint-community/eslint-utils` | 4.9.1 | MIT | development transitive | [Registry](https://registry.npmjs.org/@eslint-community/eslint-utils/4.9.1) |
| `@eslint-community/regexpp` | 4.12.2 | MIT | development transitive | [Registry](https://registry.npmjs.org/@eslint-community/regexpp/4.12.2) |
| `@eslint/config-array` | 0.21.1 | Apache-2.0 | development transitive | [Registry](https://registry.npmjs.org/@eslint/config-array/0.21.1) |
| `@eslint/config-helpers` | 0.4.2 | Apache-2.0 | development transitive | [Registry](https://registry.npmjs.org/@eslint/config-helpers/0.4.2) |
| `@eslint/core` | 0.17.0 | Apache-2.0 | development transitive | [Registry](https://registry.npmjs.org/@eslint/core/0.17.0) |
| `@eslint/eslintrc` | 3.3.4 | MIT | development transitive | [Registry](https://registry.npmjs.org/@eslint/eslintrc/3.3.4) |
| `@eslint/js` | 9.39.3 | MIT | development direct | [Registry](https://registry.npmjs.org/@eslint/js/9.39.3) |
| `@eslint/object-schema` | 2.1.7 | Apache-2.0 | development transitive | [Registry](https://registry.npmjs.org/@eslint/object-schema/2.1.7) |
| `@eslint/plugin-kit` | 0.4.1 | Apache-2.0 | development transitive | [Registry](https://registry.npmjs.org/@eslint/plugin-kit/0.4.1) |
| `@exodus/bytes` | 1.15.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/@exodus/bytes/1.15.0) |
| `@gsap/react` | 2.1.2 | GSAP Standard (custom, non-permissive) | runtime direct | [Registry](https://registry.npmjs.org/@gsap/react/2.1.2) |
| `@humanfs/core` | 0.19.1 | Apache-2.0 | development transitive | [Registry](https://registry.npmjs.org/@humanfs/core/0.19.1) |
| `@humanfs/node` | 0.16.7 | Apache-2.0 | development transitive | [Registry](https://registry.npmjs.org/@humanfs/node/0.16.7) |
| `@humanwhocodes/module-importer` | 1.0.1 | Apache-2.0 | development transitive | [Registry](https://registry.npmjs.org/@humanwhocodes/module-importer/1.0.1) |
| `@humanwhocodes/retry` | 0.4.3 | Apache-2.0 | development transitive | [Registry](https://registry.npmjs.org/@humanwhocodes/retry/0.4.3) |
| `@jridgewell/gen-mapping` | 0.3.13 | MIT | development transitive | [Registry](https://registry.npmjs.org/@jridgewell/gen-mapping/0.3.13) |
| `@jridgewell/remapping` | 2.3.5 | MIT | development transitive | [Registry](https://registry.npmjs.org/@jridgewell/remapping/2.3.5) |
| `@jridgewell/resolve-uri` | 3.1.2 | MIT | development transitive | [Registry](https://registry.npmjs.org/@jridgewell/resolve-uri/3.1.2) |
| `@jridgewell/sourcemap-codec` | 1.5.5 | MIT | development transitive | [Registry](https://registry.npmjs.org/@jridgewell/sourcemap-codec/1.5.5) |
| `@jridgewell/trace-mapping` | 0.3.31 | MIT | development transitive | [Registry](https://registry.npmjs.org/@jridgewell/trace-mapping/0.3.31) |
| `@microsoft/signalr` | 10.0.0 | MIT | runtime direct | [Registry](https://registry.npmjs.org/@microsoft/signalr/10.0.0) |
| `@playwright/test` | 1.59.1 | Apache-2.0 | development direct | [Registry](https://registry.npmjs.org/@playwright/test/1.59.1) |
| `@rolldown/pluginutils` | 1.0.0-rc.3 | MIT | development transitive | [Registry](https://registry.npmjs.org/@rolldown/pluginutils/1.0.0-rc.3) |
| `@rollup/pluginutils` | 5.3.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/@rollup/pluginutils/5.3.0) |
| `@rollup/rollup-android-arm-eabi` | 4.59.0 | MIT | development transitive, optional | [Registry](https://registry.npmjs.org/@rollup/rollup-android-arm-eabi/4.59.0) |
| `@rollup/rollup-android-arm64` | 4.59.0 | MIT | development transitive, optional | [Registry](https://registry.npmjs.org/@rollup/rollup-android-arm64/4.59.0) |
| `@rollup/rollup-darwin-arm64` | 4.59.0 | MIT | development transitive, optional | [Registry](https://registry.npmjs.org/@rollup/rollup-darwin-arm64/4.59.0) |
| `@rollup/rollup-darwin-x64` | 4.59.0 | MIT | development transitive, optional | [Registry](https://registry.npmjs.org/@rollup/rollup-darwin-x64/4.59.0) |
| `@rollup/rollup-freebsd-arm64` | 4.59.0 | MIT | development transitive, optional | [Registry](https://registry.npmjs.org/@rollup/rollup-freebsd-arm64/4.59.0) |
| `@rollup/rollup-freebsd-x64` | 4.59.0 | MIT | development transitive, optional | [Registry](https://registry.npmjs.org/@rollup/rollup-freebsd-x64/4.59.0) |
| `@rollup/rollup-linux-arm-gnueabihf` | 4.59.0 | MIT | development transitive, optional | [Registry](https://registry.npmjs.org/@rollup/rollup-linux-arm-gnueabihf/4.59.0) |
| `@rollup/rollup-linux-arm-musleabihf` | 4.59.0 | MIT | development transitive, optional | [Registry](https://registry.npmjs.org/@rollup/rollup-linux-arm-musleabihf/4.59.0) |
| `@rollup/rollup-linux-arm64-gnu` | 4.59.0 | MIT | development transitive, optional | [Registry](https://registry.npmjs.org/@rollup/rollup-linux-arm64-gnu/4.59.0) |
| `@rollup/rollup-linux-arm64-musl` | 4.59.0 | MIT | development transitive, optional | [Registry](https://registry.npmjs.org/@rollup/rollup-linux-arm64-musl/4.59.0) |
| `@rollup/rollup-linux-loong64-gnu` | 4.59.0 | MIT | development transitive, optional | [Registry](https://registry.npmjs.org/@rollup/rollup-linux-loong64-gnu/4.59.0) |
| `@rollup/rollup-linux-loong64-musl` | 4.59.0 | MIT | development transitive, optional | [Registry](https://registry.npmjs.org/@rollup/rollup-linux-loong64-musl/4.59.0) |
| `@rollup/rollup-linux-ppc64-gnu` | 4.59.0 | MIT | development transitive, optional | [Registry](https://registry.npmjs.org/@rollup/rollup-linux-ppc64-gnu/4.59.0) |
| `@rollup/rollup-linux-ppc64-musl` | 4.59.0 | MIT | development transitive, optional | [Registry](https://registry.npmjs.org/@rollup/rollup-linux-ppc64-musl/4.59.0) |
| `@rollup/rollup-linux-riscv64-gnu` | 4.59.0 | MIT | development transitive, optional | [Registry](https://registry.npmjs.org/@rollup/rollup-linux-riscv64-gnu/4.59.0) |
| `@rollup/rollup-linux-riscv64-musl` | 4.59.0 | MIT | development transitive, optional | [Registry](https://registry.npmjs.org/@rollup/rollup-linux-riscv64-musl/4.59.0) |
| `@rollup/rollup-linux-s390x-gnu` | 4.59.0 | MIT | development transitive, optional | [Registry](https://registry.npmjs.org/@rollup/rollup-linux-s390x-gnu/4.59.0) |
| `@rollup/rollup-linux-x64-gnu` | 4.59.0 | MIT | development transitive, optional | [Registry](https://registry.npmjs.org/@rollup/rollup-linux-x64-gnu/4.59.0) |
| `@rollup/rollup-linux-x64-musl` | 4.59.0 | MIT | development transitive, optional | [Registry](https://registry.npmjs.org/@rollup/rollup-linux-x64-musl/4.59.0) |
| `@rollup/rollup-openbsd-x64` | 4.59.0 | MIT | development transitive, optional | [Registry](https://registry.npmjs.org/@rollup/rollup-openbsd-x64/4.59.0) |
| `@rollup/rollup-openharmony-arm64` | 4.59.0 | MIT | development transitive, optional | [Registry](https://registry.npmjs.org/@rollup/rollup-openharmony-arm64/4.59.0) |
| `@rollup/rollup-win32-arm64-msvc` | 4.59.0 | MIT | development transitive, optional | [Registry](https://registry.npmjs.org/@rollup/rollup-win32-arm64-msvc/4.59.0) |
| `@rollup/rollup-win32-ia32-msvc` | 4.59.0 | MIT | development transitive, optional | [Registry](https://registry.npmjs.org/@rollup/rollup-win32-ia32-msvc/4.59.0) |
| `@rollup/rollup-win32-x64-gnu` | 4.59.0 | MIT | development transitive, optional | [Registry](https://registry.npmjs.org/@rollup/rollup-win32-x64-gnu/4.59.0) |
| `@rollup/rollup-win32-x64-msvc` | 4.59.0 | MIT | development transitive, optional | [Registry](https://registry.npmjs.org/@rollup/rollup-win32-x64-msvc/4.59.0) |
| `@standard-schema/spec` | 1.1.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/@standard-schema/spec/1.1.0) |
| `@svgr/babel-plugin-add-jsx-attribute` | 8.0.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/@svgr/babel-plugin-add-jsx-attribute/8.0.0) |
| `@svgr/babel-plugin-remove-jsx-attribute` | 8.0.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/@svgr/babel-plugin-remove-jsx-attribute/8.0.0) |
| `@svgr/babel-plugin-remove-jsx-empty-expression` | 8.0.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/@svgr/babel-plugin-remove-jsx-empty-expression/8.0.0) |
| `@svgr/babel-plugin-replace-jsx-attribute-value` | 8.0.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/@svgr/babel-plugin-replace-jsx-attribute-value/8.0.0) |
| `@svgr/babel-plugin-svg-dynamic-title` | 8.0.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/@svgr/babel-plugin-svg-dynamic-title/8.0.0) |
| `@svgr/babel-plugin-svg-em-dimensions` | 8.0.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/@svgr/babel-plugin-svg-em-dimensions/8.0.0) |
| `@svgr/babel-plugin-transform-react-native-svg` | 8.1.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/@svgr/babel-plugin-transform-react-native-svg/8.1.0) |
| `@svgr/babel-plugin-transform-svg-component` | 8.0.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/@svgr/babel-plugin-transform-svg-component/8.0.0) |
| `@svgr/babel-preset` | 8.1.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/@svgr/babel-preset/8.1.0) |
| `@svgr/core` | 8.1.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/@svgr/core/8.1.0) |
| `@svgr/hast-util-to-babel-ast` | 8.0.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/@svgr/hast-util-to-babel-ast/8.0.0) |
| `@svgr/plugin-jsx` | 8.1.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/@svgr/plugin-jsx/8.1.0) |
| `@testing-library/dom` | 10.4.1 | MIT | development transitive | [Registry](https://registry.npmjs.org/@testing-library/dom/10.4.1) |
| `@testing-library/jest-dom` | 6.9.1 | MIT | development direct | [Registry](https://registry.npmjs.org/@testing-library/jest-dom/6.9.1) |
| `@testing-library/react` | 16.3.2 | MIT | development direct | [Registry](https://registry.npmjs.org/@testing-library/react/16.3.2) |
| `@testing-library/user-event` | 14.6.1 | MIT | development direct | [Registry](https://registry.npmjs.org/@testing-library/user-event/14.6.1) |
| `@types/aria-query` | 5.0.4 | MIT | development transitive | [Registry](https://registry.npmjs.org/@types/aria-query/5.0.4) |
| `@types/babel__core` | 7.20.5 | MIT | development transitive | [Registry](https://registry.npmjs.org/@types/babel__core/7.20.5) |
| `@types/babel__generator` | 7.27.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/@types/babel__generator/7.27.0) |
| `@types/babel__template` | 7.4.4 | MIT | development transitive | [Registry](https://registry.npmjs.org/@types/babel__template/7.4.4) |
| `@types/babel__traverse` | 7.28.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/@types/babel__traverse/7.28.0) |
| `@types/chai` | 5.2.3 | MIT | development transitive | [Registry](https://registry.npmjs.org/@types/chai/5.2.3) |
| `@types/deep-eql` | 4.0.2 | MIT | development transitive | [Registry](https://registry.npmjs.org/@types/deep-eql/4.0.2) |
| `@types/estree` | 1.0.8 | MIT | development transitive | [Registry](https://registry.npmjs.org/@types/estree/1.0.8) |
| `@types/json-schema` | 7.0.15 | MIT | development transitive | [Registry](https://registry.npmjs.org/@types/json-schema/7.0.15) |
| `@types/node` | 24.10.15 | MIT | development direct | [Registry](https://registry.npmjs.org/@types/node/24.10.15) |
| `@types/react` | 19.2.14 | MIT | development direct | [Registry](https://registry.npmjs.org/@types/react/19.2.14) |
| `@types/react-dom` | 19.2.3 | MIT | development direct | [Registry](https://registry.npmjs.org/@types/react-dom/19.2.3) |
| `@typescript-eslint/eslint-plugin` | 8.56.1 | MIT | development transitive | [Registry](https://registry.npmjs.org/@typescript-eslint/eslint-plugin/8.56.1) |
| `@typescript-eslint/parser` | 8.56.1 | MIT | development transitive | [Registry](https://registry.npmjs.org/@typescript-eslint/parser/8.56.1) |
| `@typescript-eslint/project-service` | 8.56.1 | MIT | development transitive | [Registry](https://registry.npmjs.org/@typescript-eslint/project-service/8.56.1) |
| `@typescript-eslint/scope-manager` | 8.56.1 | MIT | development transitive | [Registry](https://registry.npmjs.org/@typescript-eslint/scope-manager/8.56.1) |
| `@typescript-eslint/tsconfig-utils` | 8.56.1 | MIT | development transitive | [Registry](https://registry.npmjs.org/@typescript-eslint/tsconfig-utils/8.56.1) |
| `@typescript-eslint/type-utils` | 8.56.1 | MIT | development transitive | [Registry](https://registry.npmjs.org/@typescript-eslint/type-utils/8.56.1) |
| `@typescript-eslint/types` | 8.56.1 | MIT | development transitive | [Registry](https://registry.npmjs.org/@typescript-eslint/types/8.56.1) |
| `@typescript-eslint/typescript-estree` | 8.56.1 | MIT | development transitive | [Registry](https://registry.npmjs.org/@typescript-eslint/typescript-estree/8.56.1) |
| `@typescript-eslint/utils` | 8.56.1 | MIT | development transitive | [Registry](https://registry.npmjs.org/@typescript-eslint/utils/8.56.1) |
| `@typescript-eslint/visitor-keys` | 8.56.1 | MIT | development transitive | [Registry](https://registry.npmjs.org/@typescript-eslint/visitor-keys/8.56.1) |
| `@vitejs/plugin-react` | 5.1.4 | MIT | development direct | [Registry](https://registry.npmjs.org/@vitejs/plugin-react/5.1.4) |
| `@vitest/expect` | 4.1.5 | MIT | development transitive | [Registry](https://registry.npmjs.org/@vitest/expect/4.1.5) |
| `@vitest/mocker` | 4.1.5 | MIT | development transitive | [Registry](https://registry.npmjs.org/@vitest/mocker/4.1.5) |
| `@vitest/pretty-format` | 4.1.5 | MIT | development transitive | [Registry](https://registry.npmjs.org/@vitest/pretty-format/4.1.5) |
| `@vitest/runner` | 4.1.5 | MIT | development transitive | [Registry](https://registry.npmjs.org/@vitest/runner/4.1.5) |
| `@vitest/snapshot` | 4.1.5 | MIT | development transitive | [Registry](https://registry.npmjs.org/@vitest/snapshot/4.1.5) |
| `@vitest/spy` | 4.1.5 | MIT | development transitive | [Registry](https://registry.npmjs.org/@vitest/spy/4.1.5) |
| `@vitest/utils` | 4.1.5 | MIT | development transitive | [Registry](https://registry.npmjs.org/@vitest/utils/4.1.5) |
| `abort-controller` | 3.0.0 | MIT | runtime transitive | [Registry](https://registry.npmjs.org/abort-controller/3.0.0) |
| `acorn` | 8.16.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/acorn/8.16.0) |
| `acorn-jsx` | 5.3.2 | MIT | development transitive | [Registry](https://registry.npmjs.org/acorn-jsx/5.3.2) |
| `ajv` | 6.14.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/ajv/6.14.0) |
| `ansi-regex` | 5.0.1 | MIT | development transitive | [Registry](https://registry.npmjs.org/ansi-regex/5.0.1) |
| `ansi-styles` | 4.3.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/ansi-styles/4.3.0) |
| `ansi-styles` | 5.2.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/ansi-styles/5.2.0) |
| `argparse` | 2.0.1 | Python-2.0 | development transitive | [Registry](https://registry.npmjs.org/argparse/2.0.1) |
| `aria-query` | 5.3.0 | Apache-2.0 | development transitive | [Registry](https://registry.npmjs.org/aria-query/5.3.0) |
| `assertion-error` | 2.0.1 | MIT | development transitive | [Registry](https://registry.npmjs.org/assertion-error/2.0.1) |
| `balanced-match` | 1.0.2 | MIT | development transitive | [Registry](https://registry.npmjs.org/balanced-match/1.0.2) |
| `balanced-match` | 4.0.4 | MIT | development transitive | [Registry](https://registry.npmjs.org/balanced-match/4.0.4) |
| `baseline-browser-mapping` | 2.10.0 | Apache-2.0 | development transitive | [Registry](https://registry.npmjs.org/baseline-browser-mapping/2.10.0) |
| `bidi-js` | 1.0.3 | MIT | development transitive | [Registry](https://registry.npmjs.org/bidi-js/1.0.3) |
| `brace-expansion` | 1.1.12 | MIT | development transitive | [Registry](https://registry.npmjs.org/brace-expansion/1.1.12) |
| `brace-expansion` | 5.0.4 | MIT | development transitive | [Registry](https://registry.npmjs.org/brace-expansion/5.0.4) |
| `browserslist` | 4.28.1 | MIT | development transitive | [Registry](https://registry.npmjs.org/browserslist/4.28.1) |
| `callsites` | 3.1.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/callsites/3.1.0) |
| `camelcase` | 6.3.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/camelcase/6.3.0) |
| `caniuse-lite` | 1.0.30001774 | CC-BY-4.0 | development transitive | [Registry](https://registry.npmjs.org/caniuse-lite/1.0.30001774) |
| `chai` | 6.2.2 | MIT | development transitive | [Registry](https://registry.npmjs.org/chai/6.2.2) |
| `chalk` | 4.1.2 | MIT | development transitive | [Registry](https://registry.npmjs.org/chalk/4.1.2) |
| `color-convert` | 2.0.1 | MIT | development transitive | [Registry](https://registry.npmjs.org/color-convert/2.0.1) |
| `color-name` | 1.1.4 | MIT | development transitive | [Registry](https://registry.npmjs.org/color-name/1.1.4) |
| `concat-map` | 0.0.1 | MIT | development transitive | [Registry](https://registry.npmjs.org/concat-map/0.0.1) |
| `convert-source-map` | 2.0.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/convert-source-map/2.0.0) |
| `cosmiconfig` | 8.3.6 | MIT | development transitive | [Registry](https://registry.npmjs.org/cosmiconfig/8.3.6) |
| `cross-spawn` | 7.0.6 | MIT | development transitive | [Registry](https://registry.npmjs.org/cross-spawn/7.0.6) |
| `css-tree` | 3.2.1 | MIT | development transitive | [Registry](https://registry.npmjs.org/css-tree/3.2.1) |
| `css.escape` | 1.5.1 | MIT | development transitive | [Registry](https://registry.npmjs.org/css.escape/1.5.1) |
| `csstype` | 3.2.3 | MIT | development transitive | [Registry](https://registry.npmjs.org/csstype/3.2.3) |
| `data-urls` | 7.0.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/data-urls/7.0.0) |
| `debug` | 4.4.3 | MIT | development transitive | [Registry](https://registry.npmjs.org/debug/4.4.3) |
| `decimal.js` | 10.6.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/decimal.js/10.6.0) |
| `deep-is` | 0.1.4 | MIT | development transitive | [Registry](https://registry.npmjs.org/deep-is/0.1.4) |
| `dequal` | 2.0.3 | MIT | development transitive | [Registry](https://registry.npmjs.org/dequal/2.0.3) |
| `detect-libc` | 2.1.2 | Apache-2.0 | development transitive, optional | [Registry](https://registry.npmjs.org/detect-libc/2.1.2) |
| `dom-accessibility-api` | 0.5.16 | MIT | development transitive | [Registry](https://registry.npmjs.org/dom-accessibility-api/0.5.16) |
| `dom-accessibility-api` | 0.6.3 | MIT | development transitive | [Registry](https://registry.npmjs.org/dom-accessibility-api/0.6.3) |
| `dot-case` | 3.0.4 | MIT | development transitive | [Registry](https://registry.npmjs.org/dot-case/3.0.4) |
| `electron-to-chromium` | 1.5.302 | ISC | development transitive | [Registry](https://registry.npmjs.org/electron-to-chromium/1.5.302) |
| `entities` | 4.5.0 | BSD-2-Clause | development transitive | [Registry](https://registry.npmjs.org/entities/4.5.0) |
| `entities` | 8.0.0 | BSD-2-Clause | development transitive | [Registry](https://registry.npmjs.org/entities/8.0.0) |
| `error-ex` | 1.3.4 | MIT | development transitive | [Registry](https://registry.npmjs.org/error-ex/1.3.4) |
| `es-module-lexer` | 2.1.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/es-module-lexer/2.1.0) |
| `esbuild` | 0.27.3 | MIT | development transitive | [Registry](https://registry.npmjs.org/esbuild/0.27.3) |
| `escalade` | 3.2.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/escalade/3.2.0) |
| `escape-string-regexp` | 4.0.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/escape-string-regexp/4.0.0) |
| `eslint` | 9.39.3 | MIT | development direct | [Registry](https://registry.npmjs.org/eslint/9.39.3) |
| `eslint-plugin-react-hooks` | 7.0.1 | MIT | development direct | [Registry](https://registry.npmjs.org/eslint-plugin-react-hooks/7.0.1) |
| `eslint-plugin-react-refresh` | 0.4.26 | MIT | development direct | [Registry](https://registry.npmjs.org/eslint-plugin-react-refresh/0.4.26) |
| `eslint-scope` | 8.4.0 | BSD-2-Clause | development transitive | [Registry](https://registry.npmjs.org/eslint-scope/8.4.0) |
| `eslint-visitor-keys` | 3.4.3 | Apache-2.0 | development transitive | [Registry](https://registry.npmjs.org/eslint-visitor-keys/3.4.3) |
| `eslint-visitor-keys` | 4.2.1 | Apache-2.0 | development transitive | [Registry](https://registry.npmjs.org/eslint-visitor-keys/4.2.1) |
| `eslint-visitor-keys` | 5.0.1 | Apache-2.0 | development transitive | [Registry](https://registry.npmjs.org/eslint-visitor-keys/5.0.1) |
| `espree` | 10.4.0 | BSD-2-Clause | development transitive | [Registry](https://registry.npmjs.org/espree/10.4.0) |
| `esquery` | 1.7.0 | BSD-3-Clause | development transitive | [Registry](https://registry.npmjs.org/esquery/1.7.0) |
| `esrecurse` | 4.3.0 | BSD-2-Clause | development transitive | [Registry](https://registry.npmjs.org/esrecurse/4.3.0) |
| `estraverse` | 5.3.0 | BSD-2-Clause | development transitive | [Registry](https://registry.npmjs.org/estraverse/5.3.0) |
| `estree-walker` | 2.0.2 | MIT | development transitive | [Registry](https://registry.npmjs.org/estree-walker/2.0.2) |
| `estree-walker` | 3.0.3 | MIT | development transitive | [Registry](https://registry.npmjs.org/estree-walker/3.0.3) |
| `esutils` | 2.0.3 | BSD-2-Clause | development transitive | [Registry](https://registry.npmjs.org/esutils/2.0.3) |
| `event-target-shim` | 5.0.1 | MIT | runtime transitive | [Registry](https://registry.npmjs.org/event-target-shim/5.0.1) |
| `eventsource` | 2.0.2 | MIT | runtime transitive | [Registry](https://registry.npmjs.org/eventsource/2.0.2) |
| `expect-type` | 1.3.0 | Apache-2.0 | development transitive | [Registry](https://registry.npmjs.org/expect-type/1.3.0) |
| `fast-deep-equal` | 3.1.3 | MIT | development transitive | [Registry](https://registry.npmjs.org/fast-deep-equal/3.1.3) |
| `fast-json-stable-stringify` | 2.1.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/fast-json-stable-stringify/2.1.0) |
| `fast-levenshtein` | 2.0.6 | MIT | development transitive | [Registry](https://registry.npmjs.org/fast-levenshtein/2.0.6) |
| `fdir` | 6.5.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/fdir/6.5.0) |
| `fetch-cookie` | 2.2.0 | Unlicense | runtime transitive | [Registry](https://registry.npmjs.org/fetch-cookie/2.2.0) |
| `file-entry-cache` | 8.0.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/file-entry-cache/8.0.0) |
| `find-up` | 5.0.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/find-up/5.0.0) |
| `flat-cache` | 4.0.1 | MIT | development transitive | [Registry](https://registry.npmjs.org/flat-cache/4.0.1) |
| `flatted` | 3.3.3 | ISC | development transitive | [Registry](https://registry.npmjs.org/flatted/3.3.3) |
| `fsevents` | 2.3.2 | MIT | development transitive, optional | [Registry](https://registry.npmjs.org/fsevents/2.3.2) |
| `fsevents` | 2.3.3 | MIT | development transitive, optional | [Registry](https://registry.npmjs.org/fsevents/2.3.3) |
| `gensync` | 1.0.0-beta.2 | MIT | development transitive | [Registry](https://registry.npmjs.org/gensync/1.0.0-beta.2) |
| `glob-parent` | 6.0.2 | ISC | development transitive | [Registry](https://registry.npmjs.org/glob-parent/6.0.2) |
| `globals` | 14.0.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/globals/14.0.0) |
| `globals` | 16.5.0 | MIT | development direct | [Registry](https://registry.npmjs.org/globals/16.5.0) |
| `gsap` | 3.15.0 | GSAP Standard (custom, non-permissive) | runtime direct | [Registry](https://registry.npmjs.org/gsap/3.15.0) |
| `has-flag` | 4.0.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/has-flag/4.0.0) |
| `hermes-estree` | 0.25.1 | MIT | development transitive | [Registry](https://registry.npmjs.org/hermes-estree/0.25.1) |
| `hermes-parser` | 0.25.1 | MIT | development transitive | [Registry](https://registry.npmjs.org/hermes-parser/0.25.1) |
| `html-encoding-sniffer` | 6.0.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/html-encoding-sniffer/6.0.0) |
| `ignore` | 5.3.2 | MIT | development transitive | [Registry](https://registry.npmjs.org/ignore/5.3.2) |
| `ignore` | 7.0.5 | MIT | development transitive | [Registry](https://registry.npmjs.org/ignore/7.0.5) |
| `import-fresh` | 3.3.1 | MIT | development transitive | [Registry](https://registry.npmjs.org/import-fresh/3.3.1) |
| `imurmurhash` | 0.1.4 | MIT | development transitive | [Registry](https://registry.npmjs.org/imurmurhash/0.1.4) |
| `indent-string` | 4.0.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/indent-string/4.0.0) |
| `is-arrayish` | 0.2.1 | MIT | development transitive | [Registry](https://registry.npmjs.org/is-arrayish/0.2.1) |
| `is-extglob` | 2.1.1 | MIT | development transitive | [Registry](https://registry.npmjs.org/is-extglob/2.1.1) |
| `is-glob` | 4.0.3 | MIT | development transitive | [Registry](https://registry.npmjs.org/is-glob/4.0.3) |
| `is-potential-custom-element-name` | 1.0.1 | MIT | development transitive | [Registry](https://registry.npmjs.org/is-potential-custom-element-name/1.0.1) |
| `isexe` | 2.0.0 | ISC | development transitive | [Registry](https://registry.npmjs.org/isexe/2.0.0) |
| `jiti` | 2.7.0 | MIT | development transitive, optional | [Registry](https://registry.npmjs.org/jiti/2.7.0) |
| `js-tokens` | 4.0.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/js-tokens/4.0.0) |
| `js-yaml` | 4.1.1 | MIT | development transitive | [Registry](https://registry.npmjs.org/js-yaml/4.1.1) |
| `jsdom` | 29.1.1 | MIT | development direct | [Registry](https://registry.npmjs.org/jsdom/29.1.1) |
| `jsesc` | 3.1.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/jsesc/3.1.0) |
| `json-buffer` | 3.0.1 | MIT | development transitive | [Registry](https://registry.npmjs.org/json-buffer/3.0.1) |
| `json-parse-even-better-errors` | 2.3.1 | MIT | development transitive | [Registry](https://registry.npmjs.org/json-parse-even-better-errors/2.3.1) |
| `json-schema-traverse` | 0.4.1 | MIT | development transitive | [Registry](https://registry.npmjs.org/json-schema-traverse/0.4.1) |
| `json-stable-stringify-without-jsonify` | 1.0.1 | MIT | development transitive | [Registry](https://registry.npmjs.org/json-stable-stringify-without-jsonify/1.0.1) |
| `json5` | 2.2.3 | MIT | development transitive | [Registry](https://registry.npmjs.org/json5/2.2.3) |
| `keyv` | 4.5.4 | MIT | development transitive | [Registry](https://registry.npmjs.org/keyv/4.5.4) |
| `levn` | 0.4.1 | MIT | development transitive | [Registry](https://registry.npmjs.org/levn/0.4.1) |
| `lightningcss` | 1.32.0 | MPL-2.0 | development transitive, optional | [Registry](https://registry.npmjs.org/lightningcss/1.32.0) |
| `lightningcss-android-arm64` | 1.32.0 | MPL-2.0 | development transitive, optional | [Registry](https://registry.npmjs.org/lightningcss-android-arm64/1.32.0) |
| `lightningcss-darwin-arm64` | 1.32.0 | MPL-2.0 | development transitive, optional | [Registry](https://registry.npmjs.org/lightningcss-darwin-arm64/1.32.0) |
| `lightningcss-darwin-x64` | 1.32.0 | MPL-2.0 | development transitive, optional | [Registry](https://registry.npmjs.org/lightningcss-darwin-x64/1.32.0) |
| `lightningcss-freebsd-x64` | 1.32.0 | MPL-2.0 | development transitive, optional | [Registry](https://registry.npmjs.org/lightningcss-freebsd-x64/1.32.0) |
| `lightningcss-linux-arm-gnueabihf` | 1.32.0 | MPL-2.0 | development transitive, optional | [Registry](https://registry.npmjs.org/lightningcss-linux-arm-gnueabihf/1.32.0) |
| `lightningcss-linux-arm64-gnu` | 1.32.0 | MPL-2.0 | development transitive, optional | [Registry](https://registry.npmjs.org/lightningcss-linux-arm64-gnu/1.32.0) |
| `lightningcss-linux-arm64-musl` | 1.32.0 | MPL-2.0 | development transitive, optional | [Registry](https://registry.npmjs.org/lightningcss-linux-arm64-musl/1.32.0) |
| `lightningcss-linux-x64-gnu` | 1.32.0 | MPL-2.0 | development transitive, optional | [Registry](https://registry.npmjs.org/lightningcss-linux-x64-gnu/1.32.0) |
| `lightningcss-linux-x64-musl` | 1.32.0 | MPL-2.0 | development transitive, optional | [Registry](https://registry.npmjs.org/lightningcss-linux-x64-musl/1.32.0) |
| `lightningcss-win32-arm64-msvc` | 1.32.0 | MPL-2.0 | development transitive, optional | [Registry](https://registry.npmjs.org/lightningcss-win32-arm64-msvc/1.32.0) |
| `lightningcss-win32-x64-msvc` | 1.32.0 | MPL-2.0 | development transitive, optional | [Registry](https://registry.npmjs.org/lightningcss-win32-x64-msvc/1.32.0) |
| `lines-and-columns` | 1.2.4 | MIT | development transitive | [Registry](https://registry.npmjs.org/lines-and-columns/1.2.4) |
| `locate-path` | 6.0.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/locate-path/6.0.0) |
| `lodash.merge` | 4.6.2 | MIT | development transitive | [Registry](https://registry.npmjs.org/lodash.merge/4.6.2) |
| `lower-case` | 2.0.2 | MIT | development transitive | [Registry](https://registry.npmjs.org/lower-case/2.0.2) |
| `lru-cache` | 11.3.6 | BlueOak-1.0.0 | development transitive | [Registry](https://registry.npmjs.org/lru-cache/11.3.6) |
| `lru-cache` | 5.1.1 | ISC | development transitive | [Registry](https://registry.npmjs.org/lru-cache/5.1.1) |
| `lz-string` | 1.5.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/lz-string/1.5.0) |
| `magic-string` | 0.30.21 | MIT | development transitive | [Registry](https://registry.npmjs.org/magic-string/0.30.21) |
| `mdn-data` | 2.27.1 | CC0-1.0 | development transitive | [Registry](https://registry.npmjs.org/mdn-data/2.27.1) |
| `min-indent` | 1.0.1 | MIT | development transitive | [Registry](https://registry.npmjs.org/min-indent/1.0.1) |
| `minimatch` | 10.2.4 | BlueOak-1.0.0 | development transitive | [Registry](https://registry.npmjs.org/minimatch/10.2.4) |
| `minimatch` | 3.1.5 | ISC | development transitive | [Registry](https://registry.npmjs.org/minimatch/3.1.5) |
| `ms` | 2.1.3 | MIT | development transitive | [Registry](https://registry.npmjs.org/ms/2.1.3) |
| `nanoid` | 3.3.11 | MIT | development transitive | [Registry](https://registry.npmjs.org/nanoid/3.3.11) |
| `natural-compare` | 1.4.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/natural-compare/1.4.0) |
| `no-case` | 3.0.4 | MIT | development transitive | [Registry](https://registry.npmjs.org/no-case/3.0.4) |
| `node-fetch` | 2.7.0 | MIT | runtime transitive | [Registry](https://registry.npmjs.org/node-fetch/2.7.0) |
| `node-releases` | 2.0.27 | MIT | development transitive | [Registry](https://registry.npmjs.org/node-releases/2.0.27) |
| `obug` | 2.1.1 | MIT | development transitive | [Registry](https://registry.npmjs.org/obug/2.1.1) |
| `optionator` | 0.9.4 | MIT | development transitive | [Registry](https://registry.npmjs.org/optionator/0.9.4) |
| `p-limit` | 3.1.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/p-limit/3.1.0) |
| `p-locate` | 5.0.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/p-locate/5.0.0) |
| `parent-module` | 1.0.1 | MIT | development transitive | [Registry](https://registry.npmjs.org/parent-module/1.0.1) |
| `parse-json` | 5.2.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/parse-json/5.2.0) |
| `parse5` | 8.0.1 | MIT | development transitive | [Registry](https://registry.npmjs.org/parse5/8.0.1) |
| `path-exists` | 4.0.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/path-exists/4.0.0) |
| `path-key` | 3.1.1 | MIT | development transitive | [Registry](https://registry.npmjs.org/path-key/3.1.1) |
| `path-type` | 4.0.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/path-type/4.0.0) |
| `pathe` | 2.0.3 | MIT | development transitive | [Registry](https://registry.npmjs.org/pathe/2.0.3) |
| `picocolors` | 1.1.1 | ISC | development transitive | [Registry](https://registry.npmjs.org/picocolors/1.1.1) |
| `picomatch` | 4.0.3 | MIT | development transitive | [Registry](https://registry.npmjs.org/picomatch/4.0.3) |
| `playwright` | 1.59.1 | Apache-2.0 | development transitive | [Registry](https://registry.npmjs.org/playwright/1.59.1) |
| `playwright-core` | 1.59.1 | Apache-2.0 | development transitive | [Registry](https://registry.npmjs.org/playwright-core/1.59.1) |
| `postcss` | 8.5.6 | MIT | development transitive | [Registry](https://registry.npmjs.org/postcss/8.5.6) |
| `prelude-ls` | 1.2.1 | MIT | development transitive | [Registry](https://registry.npmjs.org/prelude-ls/1.2.1) |
| `pretty-format` | 27.5.1 | MIT | development transitive | [Registry](https://registry.npmjs.org/pretty-format/27.5.1) |
| `psl` | 1.15.0 | MIT | runtime transitive | [Registry](https://registry.npmjs.org/psl/1.15.0) |
| `punycode` | 2.3.1 | MIT | runtime transitive | [Registry](https://registry.npmjs.org/punycode/2.3.1) |
| `querystringify` | 2.2.0 | MIT | runtime transitive | [Registry](https://registry.npmjs.org/querystringify/2.2.0) |
| `react` | 19.2.4 | MIT | runtime direct | [Registry](https://registry.npmjs.org/react/19.2.4) |
| `react-dom` | 19.2.4 | MIT | runtime direct | [Registry](https://registry.npmjs.org/react-dom/19.2.4) |
| `react-is` | 17.0.2 | MIT | development transitive | [Registry](https://registry.npmjs.org/react-is/17.0.2) |
| `react-refresh` | 0.18.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/react-refresh/0.18.0) |
| `redent` | 3.0.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/redent/3.0.0) |
| `require-from-string` | 2.0.2 | MIT | development transitive | [Registry](https://registry.npmjs.org/require-from-string/2.0.2) |
| `requires-port` | 1.0.0 | MIT | runtime transitive | [Registry](https://registry.npmjs.org/requires-port/1.0.0) |
| `resolve-from` | 4.0.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/resolve-from/4.0.0) |
| `rollup` | 4.59.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/rollup/4.59.0) |
| `saxes` | 6.0.0 | ISC | development transitive | [Registry](https://registry.npmjs.org/saxes/6.0.0) |
| `scheduler` | 0.27.0 | MIT | runtime transitive | [Registry](https://registry.npmjs.org/scheduler/0.27.0) |
| `semver` | 6.3.1 | ISC | development transitive | [Registry](https://registry.npmjs.org/semver/6.3.1) |
| `semver` | 7.7.4 | ISC | development transitive | [Registry](https://registry.npmjs.org/semver/7.7.4) |
| `set-cookie-parser` | 2.7.2 | MIT | runtime transitive | [Registry](https://registry.npmjs.org/set-cookie-parser/2.7.2) |
| `shebang-command` | 2.0.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/shebang-command/2.0.0) |
| `shebang-regex` | 3.0.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/shebang-regex/3.0.0) |
| `siginfo` | 2.0.0 | ISC | development transitive | [Registry](https://registry.npmjs.org/siginfo/2.0.0) |
| `snake-case` | 3.0.4 | MIT | development transitive | [Registry](https://registry.npmjs.org/snake-case/3.0.4) |
| `source-map-js` | 1.2.1 | BSD-3-Clause | development transitive | [Registry](https://registry.npmjs.org/source-map-js/1.2.1) |
| `stackback` | 0.0.2 | MIT | development transitive | [Registry](https://registry.npmjs.org/stackback/0.0.2) |
| `std-env` | 4.1.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/std-env/4.1.0) |
| `strip-indent` | 3.0.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/strip-indent/3.0.0) |
| `strip-json-comments` | 3.1.1 | MIT | development transitive | [Registry](https://registry.npmjs.org/strip-json-comments/3.1.1) |
| `supports-color` | 7.2.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/supports-color/7.2.0) |
| `svg-parser` | 2.0.4 | MIT | development transitive | [Registry](https://registry.npmjs.org/svg-parser/2.0.4) |
| `symbol-tree` | 3.2.4 | MIT | development transitive | [Registry](https://registry.npmjs.org/symbol-tree/3.2.4) |
| `tinybench` | 2.9.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/tinybench/2.9.0) |
| `tinyexec` | 1.1.2 | MIT | development transitive | [Registry](https://registry.npmjs.org/tinyexec/1.1.2) |
| `tinyglobby` | 0.2.15 | MIT | development transitive | [Registry](https://registry.npmjs.org/tinyglobby/0.2.15) |
| `tinyrainbow` | 3.1.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/tinyrainbow/3.1.0) |
| `tldts` | 7.0.30 | MIT | development transitive | [Registry](https://registry.npmjs.org/tldts/7.0.30) |
| `tldts-core` | 7.0.30 | MIT | development transitive | [Registry](https://registry.npmjs.org/tldts-core/7.0.30) |
| `tough-cookie` | 4.1.4 | BSD-3-Clause | runtime transitive | [Registry](https://registry.npmjs.org/tough-cookie/4.1.4) |
| `tough-cookie` | 6.0.1 | BSD-3-Clause | development transitive | [Registry](https://registry.npmjs.org/tough-cookie/6.0.1) |
| `tr46` | 0.0.3 | MIT | runtime transitive | [Registry](https://registry.npmjs.org/tr46/0.0.3) |
| `tr46` | 6.0.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/tr46/6.0.0) |
| `tr46` | 6.0.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/tr46/6.0.0) |
| `ts-api-utils` | 2.4.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/ts-api-utils/2.4.0) |
| `tslib` | 2.8.1 | 0BSD | runtime transitive | [Registry](https://registry.npmjs.org/tslib/2.8.1) |
| `type-check` | 0.4.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/type-check/0.4.0) |
| `typescript` | 5.9.3 | Apache-2.0 | development direct | [Registry](https://registry.npmjs.org/typescript/5.9.3) |
| `typescript-eslint` | 8.56.1 | MIT | development direct | [Registry](https://registry.npmjs.org/typescript-eslint/8.56.1) |
| `undici` | 7.25.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/undici/7.25.0) |
| `undici-types` | 7.16.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/undici-types/7.16.0) |
| `universalify` | 0.2.0 | MIT | runtime transitive | [Registry](https://registry.npmjs.org/universalify/0.2.0) |
| `update-browserslist-db` | 1.2.3 | MIT | development transitive | [Registry](https://registry.npmjs.org/update-browserslist-db/1.2.3) |
| `uri-js` | 4.4.1 | BSD-2-Clause | development transitive | [Registry](https://registry.npmjs.org/uri-js/4.4.1) |
| `url-parse` | 1.5.10 | MIT | runtime transitive | [Registry](https://registry.npmjs.org/url-parse/1.5.10) |
| `vite` | 7.3.1 | MIT | development direct | [Registry](https://registry.npmjs.org/vite/7.3.1) |
| `vite-plugin-svgr` | 4.5.0 | MIT | development direct | [Registry](https://registry.npmjs.org/vite-plugin-svgr/4.5.0) |
| `vitest` | 4.1.5 | MIT | development direct | [Registry](https://registry.npmjs.org/vitest/4.1.5) |
| `w3c-xmlserializer` | 5.0.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/w3c-xmlserializer/5.0.0) |
| `webidl-conversions` | 3.0.1 | BSD-2-Clause | runtime transitive | [Registry](https://registry.npmjs.org/webidl-conversions/3.0.1) |
| `webidl-conversions` | 8.0.1 | BSD-2-Clause | development transitive | [Registry](https://registry.npmjs.org/webidl-conversions/8.0.1) |
| `webidl-conversions` | 8.0.1 | BSD-2-Clause | development transitive | [Registry](https://registry.npmjs.org/webidl-conversions/8.0.1) |
| `whatwg-mimetype` | 5.0.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/whatwg-mimetype/5.0.0) |
| `whatwg-url` | 16.0.1 | MIT | development transitive | [Registry](https://registry.npmjs.org/whatwg-url/16.0.1) |
| `whatwg-url` | 16.0.1 | MIT | development transitive | [Registry](https://registry.npmjs.org/whatwg-url/16.0.1) |
| `whatwg-url` | 5.0.0 | MIT | runtime transitive | [Registry](https://registry.npmjs.org/whatwg-url/5.0.0) |
| `which` | 2.0.2 | ISC | development transitive | [Registry](https://registry.npmjs.org/which/2.0.2) |
| `why-is-node-running` | 2.3.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/why-is-node-running/2.3.0) |
| `word-wrap` | 1.2.5 | MIT | development transitive | [Registry](https://registry.npmjs.org/word-wrap/1.2.5) |
| `ws` | 7.5.14 | MIT | runtime transitive | [Registry](https://registry.npmjs.org/ws/7.5.14) |
| `xml-name-validator` | 5.0.0 | Apache-2.0 | development transitive | [Registry](https://registry.npmjs.org/xml-name-validator/5.0.0) |
| `xmlchars` | 2.2.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/xmlchars/2.2.0) |
| `yallist` | 3.1.1 | ISC | development transitive | [Registry](https://registry.npmjs.org/yallist/3.1.1) |
| `yocto-queue` | 0.1.0 | MIT | development transitive | [Registry](https://registry.npmjs.org/yocto-queue/0.1.0) |
| `zod` | 4.3.6 | MIT | development transitive | [Registry](https://registry.npmjs.org/zod/4.3.6) |
| `zod-validation-error` | 4.0.2 | MIT | development transitive | [Registry](https://registry.npmjs.org/zod-validation-error/4.0.2) |
