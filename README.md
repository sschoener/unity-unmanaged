There are three things in this repository.

- `UnityExposedProject` is a Unity project that produces an assembly `UnityExposed.dll` so that dependencies are correctly setup. Note that this script has a hardcoded path to a Unity DLL. You need to adapt it to your local setup to run it.
- `CecilExpose` is a script that takes said assembly and patches in some calls to internal Unity methods. You can run `dotnet restore` and `dotnet run` in this folder. Note that all paths are hardcoded and you need to manually copy the output around.
- `ExampleProject` is a Unity project that uses the new unmanaged functionality.

All Unity project require Unity 6 (6000.0.24f1).

You can just open the `ExampleProject` and see the changes in action without running any other scripts.

---

PRs welcome! Ideas for improvements:

- figure out how references need to be setup in Cecil and get rid of the `UnityExposedProject`. It should be unnecessary.
- automatically put the output of `CecilExpose` into a sensible place.
- get rid of the hardcoded paths in `CecilExpose`.
- don't handle methods one-by-one but write some actual tooling that exposes the unmanaged Unity bindings in a more systematic way.
