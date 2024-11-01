There are three things in this repository.

- `UnityExposedProject` is a Unity project that produces an assembly `UnityExposed.dll` so that dependencies are correctly setup.
- `CecilExpose` is a script that takes said assembly and patches in some calls to internal Unity methods. You can run `dotnet restore` and `dotnet run` in this folder. Note that all paths are hardcoded and you need to manually copy the output around.
- `ExampleProject` is a Unity project that uses the new unmanaged functionality.

All Unity project require Unity 6 (6000.0.24f1).

PRs welcome! Ideas for improvements:

- figure out how references need to be setup in Cecil and get rid of the `UnityExposedProject`. It should be unnecessary.
- automatically put the output of `CecilExpose` into a sensible place.
- get rid of the hardcoded paths in `CecilExpose`.
- don't handle methods one-by-one but write some actual tooling that exposes the unmanaged Unity bindings in a more systematic way.
