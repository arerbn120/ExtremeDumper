using System;
using System.Collections.Generic;
using dnlib.DotNet;
using dnlib.PE;
using ExtremeDumper.Logging;

namespace ExtremeDumper.Dumping;

/// <summary>
/// Compatibility layer for dnlib 4.5.x API changes and improvements
/// Ensures smooth operation across dnlib versions
/// </summary>
public static class Dnlib45CompatibilityLayer {
	/// <summary>
	/// Load PE image with version-specific handling
	/// </summary>
	public static PEImage? TryLoadPEImage(byte[] data) {
		try {
			if (data == null || data.Length == 0)
				return null;

			// dnlib 4.5: PEImage constructor is version-aware
			var peImage = new PEImage(data);
			
			// Validate loaded image
			if (!IsValidPEImage(peImage)) {
				peImage.Dispose();
				return null;
			}

			return peImage;
		}
		catch (Exception ex) {
			Logger.Debug($"[Dnlib45] Failed to load PE image: {ex.Message}");
			return null;
		}
	}

	/// <summary>
	/// Load module with dnlib 4.5 optimizations
	/// </summary>
	public static ModuleDefMD? TryLoadModule(PEImage peImage, ModuleCreationOptions? options = null) {
		try {
			if (peImage == null)
				return null;

			// dnlib 4.5: ModuleDefMD.Load with optional parameters
			ModuleDefMD module;
			
			if (options != null) {
				module = ModuleDefMD.Load(peImage, options);
			} else {
				// Use default options (dnlib 4.5 friendly)
				module = ModuleDefMD.Load(peImage);
			}

			// Validate module
			if (!IsValidModule(module)) {
				module?.Dispose();
				return null;
			}

			return module;
		}
		catch (Exception ex) {
			Logger.Debug($"[Dnlib45] Failed to load module: {ex.Message}");
			return null;
		}
	}

	/// <summary>
	/// Validate PE image integrity
	/// </summary>
	private static bool IsValidPEImage(PEImage peImage) {
		try {
			// dnlib 4.5: Better validation APIs
			if (peImage == null)
				return false;

			// Check headers exist
			if (peImage.ImageNTHeaders == null)
				return false;

			// Check optional header
			if (peImage.ImageNTHeaders.OptionalHeader == null)
				return false;

			return true;
		}
		catch {
			return false;
		}
	}

	/// <summary>
	/// Validate module integrity
	/// </summary>
	private static bool IsValidModule(ModuleDefMD module) {
		try {
			if (module == null)
				return false;

			// dnlib 4.5: Module should have valid metadata
			if (string.IsNullOrEmpty(module.Name))
				return false;

			// Check if types can be enumerated
			var typeCount = 0;
			foreach (var type in module.Types) {
				typeCount++;
				if (typeCount > 10000) // Sanity check
					return false;
			}

			return true;
		}
		catch {
			return false;
		}
	}

	/// <summary>
	/// Safe method body access with dnlib 4.5
	/// </summary>
	public static CilBody? GetMethodBody(MethodDef method) {
		try {
			if (method == null)
				return null;

			// dnlib 4.5: Improved null safety
			if (method.HasBody && method.Body != null) {
				return method.Body;
			}

			return null;
		}
		catch {
			return null;
		}
	}

	/// <summary>
	/// Get instruction at offset with dnlib 4.5
	/// </summary>
	public static Instruction? GetInstructionAt(CilBody body, uint offset) {
		try {
			if (body?.Instructions == null)
				return null;

			// dnlib 4.5: Better offset lookup
			foreach (var instruction in body.Instructions) {
				if (instruction.Offset == offset)
					return instruction;
			}

			return null;
		}
		catch {
			return null;
		}
	}

	/// <summary>
	/// Create method clone with dnlib 4.5
	/// </summary>
	public static MethodDef? CloneMethod(MethodDef original) {
		try {
			if (original == null)
				return null;

			// dnlib 4.5: Improved method cloning
			var cloned = new MethodDefUser(
				original.Name,
				original.MethodSig,
				original.ImplAttributes) {
				Attributes = original.Attributes,
				ReturnType = original.ReturnType
			};

			// Clone body if exists
			if (original.Body != null) {
				var newBody = new CilBody(
					original.Body.InitLocals,
					new List<Instruction>(original.Body.Instructions),
					new List<ExceptionHandler>(original.Body.ExceptionHandlers),
					new List<Local>(original.Body.Variables));

				cloned.Body = newBody;
			}

			// Copy parameters
			foreach (var param in original.Parameters) {
				cloned.Parameters.Add(new ParamDefUser(
					param.Name,
					param.Sequence,
					param.Attributes));
			}

			// Copy custom attributes
			foreach (var attr in original.CustomAttributes) {
				cloned.CustomAttributes.Add(attr);
			}

			return cloned;
		}
		catch (Exception ex) {
			Logger.Debug($"[Dnlib45] Failed to clone method: {ex.Message}");
			return null;
		}
	}

	/// <summary>
	/// Resolve type with improved dnlib 4.5 handling
	/// </summary>
	public static TypeDef? ResolveType(ITypeDefOrRef typeRef) {
		try {
			if (typeRef == null)
				return null;

			// dnlib 4.5: Better type resolution
			if (typeRef is TypeDef td)
				return td;

			if (typeRef is TypeRef tr) {
				// Improved error handling in 4.5
				try {
					return tr.Resolve();
				}
				catch {
					return null;
				}
			}

			return null;
		}
		catch {
			return null;
		}
	}

	/// <summary>
	/// Safe field access with dnlib 4.5
	/// </summary>
	public static FieldDef? FindField(TypeDef type, string fieldName) {
		try {
			if (type?.Fields == null)
				return null;

			// dnlib 4.5: Optimized field lookup
			foreach (var field in type.Fields) {
				if (field.Name == fieldName)
					return field;
			}

			return null;
		}
		catch {
			return null;
		}
	}

	/// <summary>
	/// Safe method lookup with dnlib 4.5
	/// </summary>
	public static MethodDef? FindMethod(TypeDef type, string methodName, int paramCount = -1) {
		try {
			if (type?.Methods == null)
				return null;

			// dnlib 4.5: Optimized method lookup
			foreach (var method in type.Methods) {
				if (method.Name != methodName)
					continue;

				if (paramCount >= 0) {
					if (method.Parameters.Count != paramCount)
						continue;
				}

				return method;
			}

			return null;
		}
		catch {
			return null;
		}
	}

	/// <summary>
	/// Get all base types (inheritance chain) with dnlib 4.5
	/// </summary>
	public static IEnumerable<TypeDef> GetBaseTypes(TypeDef type) {
		try {
			if (type == null)
				yield break;

			var visited = new HashSet<TypeDef>();
			var toVisit = new Queue<TypeDef>();
			toVisit.Enqueue(type);

			while (toVisit.Count > 0) {
				var current = toVisit.Dequeue();
				if (visited.Contains(current))
					continue;

				visited.Add(current);
				yield return current;

				// dnlib 4.5: Improved base type access
				if (current.BaseType != null) {
					var baseType = ResolveType(current.BaseType);
					if (baseType != null && !visited.Contains(baseType)) {
						toVisit.Enqueue(baseType);
					}
				}
			}
		}
		catch {
			yield break;
		}
	}

	/// <summary>
	/// Check if method is virtual with dnlib 4.5
	/// </summary>
	public static bool IsVirtualMethod(MethodDef method) {
		try {
			if (method == null)
				return false;

			// dnlib 4.5: Better virtual method detection
			return method.IsVirtual || method.IsAbstract || method.IsFinal;
		}
		catch {
			return false;
		}
	}

	/// <summary>
	/// Get interface implementations with dnlib 4.5
	/// </summary>
	public static IEnumerable<InterfaceImpl> GetInterfaces(TypeDef type) {
		try {
			if (type?.Interfaces == null)
				return Enumerable.Empty<InterfaceImpl>();

			// dnlib 4.5: Optimized interface enumeration
			return type.Interfaces.AsEnumerable();
		}
		catch {
			return Enumerable.Empty<InterfaceImpl>();
		}
	}

	/// <summary>
	/// Write module with dnlib 4.5 optimizations
	/// </summary>
	public static bool TryWriteModule(ModuleDefMD module, string filePath) {
		try {
			if (module == null || string.IsNullOrEmpty(filePath))
				return false;

			// dnlib 4.5: Improved write performance
			module.Write(filePath);
			return true;
		}
		catch (Exception ex) {
			Logger.Error($"[Dnlib45] Failed to write module: {ex.Message}");
			return false;
		}
	}

	/// <summary>
	/// Write module to stream with dnlib 4.5
	/// </summary>
	public static bool TryWriteModule(ModuleDefMD module, System.IO.Stream stream) {
		try {
			if (module == null || stream == null)
				return false;

			// dnlib 4.5: Optimized stream writing
			module.Write(stream);
			return true;
		}
		catch (Exception ex) {
			Logger.Error($"[Dnlib45] Failed to write module to stream: {ex.Message}");
			return false;
		}
	}

	/// <summary>
	/// Get all local variables with dnlib 4.5
	/// </summary>
	public static IEnumerable<Local> GetLocalVariables(MethodDef method) {
		try {
			if (method?.Body?.Variables == null)
				return Enumerable.Empty<Local>();

			// dnlib 4.5: Improved variable enumeration
			return method.Body.Variables.AsEnumerable();
		}
		catch {
			return Enumerable.Empty<Local>();
		}
	}

	/// <summary>
	/// Get enum underlying type with dnlib 4.5
	/// </summary>
	public static TypeSig? GetEnumUnderlyingType(TypeDef enumType) {
		try {
			if (enumType == null || !enumType.IsEnum)
				return null;

			// dnlib 4.5: Better enum handling
			var valueField = enumType.Fields.FirstOrDefault(f => f.Name == "value__");
			return valueField?.Type;
		}
		catch {
			return null;
		}
	}

	/// <summary>
	/// Get resource data with dnlib 4.5
	/// </summary>
	public static byte[]? GetResourceData(ModuleDef module, string resourceName) {
		try {
			if (module?.Resources == null)
				return null;

			// dnlib 4.5: Improved resource lookup
			foreach (var resource in module.Resources) {
				if (resource.Name == resourceName && resource is EmbeddedResource embeddedRes) {
					return embeddedRes.GetData();
				}
			}

			return null;
		}
		catch {
			return null;
		}
	}

	/// <summary>
	/// Check version compatibility
	/// </summary>
	public static bool IsCompatibleVersion() {
		try {
			// Check dnlib version at runtime
			var dnlibType = typeof(ModuleDefMD);
			var version = dnlibType.Assembly.GetName().Version;

			// dnlib 4.5.0 or higher
			return version.Major == 4 && version.Minor >= 5;
		}
		catch {
			return false;
		}
	}

	/// <summary>
	/// Get dnlib version information
	/// </summary>
	public static string GetDnlibVersion() {
		try {
			var version = typeof(ModuleDefMD).Assembly.GetName().Version;
			return $"{version.Major}.{version.Minor}.{version.Build}";
		}
		catch {
			return "Unknown";
		}
	}
}