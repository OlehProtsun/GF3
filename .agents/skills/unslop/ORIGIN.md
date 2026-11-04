# unslop

- Author: Damir Sokolovsky (@heydamir).
- Repository: https://github.com/heydamir/unslop
- Source location: `skills/unslop`
- Source SHA: `a6f121eee5f29c2f74976b75456feae8b66afb3a`
- Original and local license: CC BY 4.0. Attribution retained; upstream files unchanged.
- Target: Codex, only the GF3 repository. This is an agent instruction skill, not an application dependency.
- Bundled scripts are optional. They have not been executed or installed as startup hooks. Snapshot requires Playwright; story video additionally requires ffmpeg.

## Removal

Delete only `.agents/skills/unslop/`, stage that deletion in Git, and, if necessary, restart/reload Codex skill discovery. No global installation or runtime hooks were added.

Uninstalling the skill leaves already generated UI changes in place. Revert the separate Home-design commit to restore the old UI independently.
