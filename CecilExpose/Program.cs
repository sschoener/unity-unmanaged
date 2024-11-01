using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

class Program
{
    static void Main()
    {
        const string UnityAssemblyPath = @"F:\UnityEditors\6000.0.24f1\Editor\Data\Managed\UnityEngine\UnityEngine.CoreModule.dll";
        const string EmptyAssemblyPath = @"..\UnityExposedProject\Library\ScriptAssemblies\UnityExposed.dll";

        // Load the original assembly containing the private method
        var unityAssembly = AssemblyDefinition.ReadAssembly(UnityAssemblyPath);

        var assembly = AssemblyDefinition.ReadAssembly(EmptyAssemblyPath);

        // Generate the IL code to call UnityEngine.Texture.get_mipmapCount_Injected
        {
            var textureType = new TypeDefinition("UnityExposed", "Texture", TypeAttributes.Public | TypeAttributes.Class, assembly.MainModule.TypeSystem.Object);
            assembly.MainModule.Types.Add(textureType);

            // Define a public method in the new assembly to forward calls to the private `DoThing` method
            var mipMapGetter = new MethodDefinition(
                "get_mipmapCount_Injected",
                MethodAttributes.Public | MethodAttributes.Static,
                assembly.MainModule.TypeSystem.Int32);

            // Add parameters to the public method that match the private method signature
            mipMapGetter.Parameters.Add(new ParameterDefinition("_unity_self", ParameterAttributes.None, assembly.MainModule.TypeSystem.IntPtr));

            var ilProcessor = mipMapGetter.Body.GetILProcessor();
            ilProcessor.Emit(OpCodes.Ldarg_0);  // Load the IntPtr parameter
            {
                var privateType = unityAssembly.MainModule.GetType("UnityEngine.Texture");
                var privateMethod = privateType.Methods.First(m => m.Name == "get_mipmapCount_Injected");
                ilProcessor.Emit(OpCodes.Call, assembly.MainModule.ImportReference(privateMethod));  // Call the private method

            }
            ilProcessor.Emit(OpCodes.Ret);      // Return the result of the private method call

            // Add the public method to the public type
            textureType.Methods.Add(mipMapGetter);
        }

        var objectType = new TypeDefinition("UnityExposed", "Object", TypeAttributes.Public | TypeAttributes.Class, assembly.MainModule.TypeSystem.Object);
        assembly.MainModule.Types.Add(objectType);

        // Generate the IL code to call UnityEngine.Object.SetName_Injected
        {
            var origSpanWrapper = unityAssembly.MainModule.GetType("UnityEngine.Bindings.ManagedSpanWrapper");
            var newSpanWrapper = assembly.MainModule.GetType("UnityExposed.Bindings.ManagedSpanWrapper");

            var nameSetter = new MethodDefinition(
                "SetName_Injected",
                MethodAttributes.Public | MethodAttributes.Static,
                assembly.MainModule.TypeSystem.Void);
            nameSetter.Parameters.Add(new ParameterDefinition("_unity_self", ParameterAttributes.None, assembly.MainModule.TypeSystem.IntPtr));
            nameSetter.Parameters.Add(new ParameterDefinition("name", ParameterAttributes.None, newSpanWrapper));

            // Define a local variable of type ManagedSpanWrapper
            var originalStructVar = new VariableDefinition(assembly.MainModule.ImportReference(origSpanWrapper));
            nameSetter.Body.Variables.Add(originalStructVar);
            nameSetter.Body.InitLocals = false;

            // Generate IL for the wrapper method
            var il = nameSetter.Body.GetILProcessor();

            // Construct the local ManagedSpanWrapper
            il.Emit(OpCodes.Ldloca_S, originalStructVar);
            il.Emit(OpCodes.Ldarg_1);                 // Load the PublicManagedSpanWrapper parameter
            il.Emit(OpCodes.Ldfld, newSpanWrapper.Fields.First(f => f.Name == "begin"));   // Load 'begin' field
            il.Emit(OpCodes.Ldarg_1);                 // Load PublicManagedSpanWrapper parameter
            il.Emit(OpCodes.Ldfld, newSpanWrapper.Fields.First(f => f.Name == "length"));  // Load 'length' field
            il.Emit(OpCodes.Call, assembly.MainModule.ImportReference(origSpanWrapper.GetConstructors().First()));

            // Call SetName_Injected
            il.Emit(OpCodes.Ldarg_0);                 // Load the IntPtr parameter (_unity_self)
            il.Emit(OpCodes.Ldloca_S, originalStructVar);  // Load reference to ManagedSpanWrapper
            var privateType = unityAssembly.MainModule.GetType("UnityEngine.Object");
            var privateMethod = privateType.Methods.First(m => m.Name == "SetName_Injected");
            il.Emit(OpCodes.Call, assembly.MainModule.ImportReference(privateMethod));   // Call the original SetName_Injected method

            il.Emit(OpCodes.Ret);

            // Add the wrapper method to the target type
            objectType.Methods.Add(nameSetter);
        }

        // Generate the IL code to call UnityEngine.Object.GetPtrFromInstanceID
        // We'll generate
        // IntPtr GetPtrFromInstanceID<T>(int instanceID) where T : UnityObject
        // {
        //     bool isMonoBehaviour;
        //     return UnityEngine.Object.GetPtrFromInstanceID(instanceID, typeof(T), out isMonoBehaviour);
        // }
        {
            var marshaller = new MethodDefinition(
                "GetPtrFromInstanceID",
                MethodAttributes.Public | MethodAttributes.Static,
                assembly.MainModule.TypeSystem.IntPtr);
            var unityObjectType = unityAssembly.MainModule.GetType("UnityEngine.Object");
            marshaller.Parameters.Add(new ParameterDefinition("instanceID", ParameterAttributes.None, assembly.MainModule.TypeSystem.Int32));

            // Define a generic parameter `T` constrained to `UnityEngine.Object`
            var genericParamT = new GenericParameter("T", marshaller);
            genericParamT.Constraints.Add(new GenericParameterConstraint(assembly.MainModule.ImportReference(unityObjectType)));  // Constrain to UnityEngine.Object
            marshaller.GenericParameters.Add(genericParamT);

            // Define a local variable `isMonoBehaviour` for the out parameter
            var isMonoBehaviourVar = new VariableDefinition(assembly.MainModule.TypeSystem.Boolean);
            marshaller.Body.Variables.Add(isMonoBehaviourVar);
            marshaller.Body.InitLocals = true;

            // Generate IL to call the private method with the generic constraint
            var il = marshaller.Body.GetILProcessor();

            // Load `instanceId` parameter onto the stack
            il.Emit(OpCodes.Ldarg_0);

            {
                // Naively, we'd just like to do this:
                //    il.Emit(OpCodes.Call, assembly.MainModule.ImportReference(typeof(Type).GetMethod("GetTypeFromHandle")));
                // but this pulls in the "Type" type from the current context that is executing this script. We need the "Type"
                // type from what the libraries we already reference. I have no idea how to do this.
                // However, I know how to call a generic function that does the same thing, hence this detour.

                var getTypeMethod = assembly.MainModule.GetType("UnityExposed.TypeHelper").GetMethods().First(m => m.Name == "GetType");
                var genericVersion = new GenericInstanceMethod(getTypeMethod);
                genericVersion.GenericArguments.Add(genericParamT);
                il.Emit(OpCodes.Call, genericVersion);
            }

            // Load the address of `isMonoBehaviourVar` onto the stack for the `out` parameter
            il.Emit(OpCodes.Ldloca_S, isMonoBehaviourVar);

            // Call `UnityEngine.Object.GetPtrFromInstanceId(instanceId, typeof(T), out isMonoBehaviour)`
            {
                var privateMethod = unityObjectType.Methods.First(m => m.Name == "GetPtrFromInstanceID");
                il.Emit(OpCodes.Call, assembly.MainModule.ImportReference(privateMethod));
            }

            // Return the result (IntPtr)
            il.Emit(OpCodes.Ret);

            objectType.Methods.Add(marshaller);
        }

        // Save the new assembly
        assembly.Write("../ExampleProject/Assets/UnityUnmanaged/UnityExposed.dll");

        Console.WriteLine("Exported assembly created successfully.");
    }
}
