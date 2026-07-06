using System;
using System.IO;

namespace ExtremeDumper.Utilities;

/// <summary>
/// Common validation and safety checks
/// </summary>
public static class ValidationHelper {
	/// <summary>
	/// Validate process ID
	/// </summary>
	public static bool IsValidProcessId(uint processId) {
		return processId > 0 && processId < int.MaxValue;
	}

	/// <summary>
	/// Validate module handle
	/// </summary>
	public static bool IsValidModuleHandle(nuint handle) {
		return handle != 0 && handle != nuint.MaxValue;
	}

	/// <summary>
	/// Validate file path for writing
	/// </summary>
	public static bool CanWriteToPath(string? filePath) {
		try {
			if (string.IsNullOrWhiteSpace(filePath))
				return false;

			var directory = Path.GetDirectoryName(filePath);
			if (string.IsNullOrWhiteSpace(directory))
				return false;

			return Directory.Exists(directory) && !Path.IsPathRooted(filePath) == false;
		}
		catch {
			return false;
		}
	}

	/// <summary>
	/// Validate binary data as valid PE
	/// </summary>
	public static bool IsValidPEHeader(byte[]? data) {
		try {
			if (data == null || data.Length < 64)
				return false;

			// Check MZ header
			if (data[0] != 0x4D || data[1] != 0x5A)
				return false;

			// Check PE signature offset
			if (BitConverter.ToUInt32(data, 0x3C) > data.Length - 4)
				return false;

			// Check PE signature
			int peOffset = BitConverter.ToInt32(data, 0x3C);
			if (peOffset < 64 || peOffset > 512)
				return false;

			if (BitConverter.ToUInt32(data, peOffset) != 0x4550)
				return false;

			return true;
		}
		catch {
			return false;
		}
	}

	/// <summary>
	/// Validate .NET module by checking CLR header
	/// </summary>
	public static bool IsDotNetAssembly(byte[]? data) {
		try {
			if (!IsValidPEHeader(data))
				return false;

			// Find .NET CLR header directory entry
			const int COFF_HEADER_OFFSET = 0x3C;
			const int PE_MAGIC_OFFSET = 24;
			const int DATA_DIRECTORIES_OFFSET = 96;
			const int CLR_HEADER_INDEX = 14;
			const int DIRECTORY_ENTRY_SIZE = 8;

			int peOffset = BitConverter.ToInt32(data, COFF_HEADER_OFFSET);
			int optionalHeaderOffset = peOffset + PE_MAGIC_OFFSET;
			int dataDirectoriesOffset = optionalHeaderOffset + DATA_DIRECTORIES_OFFSET;
			int clrHeaderOffset = dataDirectoriesOffset + (CLR_HEADER_INDEX * DIRECTORY_ENTRY_SIZE);

			if (clrHeaderOffset + 8 > data!.Length)
				return false;

			uint clrHeaderRva = BitConverter.ToUInt32(data, clrHeaderOffset);
			uint clrHeaderSize = BitConverter.ToUInt32(data, clrHeaderOffset + 4);

			return clrHeaderRva != 0 && clrHeaderSize != 0;
		}
		catch {
			return false;
		}
	}

	/// <summary>
	/// Validate image layout
	/// </summary>
	public static bool IsValidImageLayout(dnlib.PE.ImageLayout layout) {
		return layout == dnlib.PE.ImageLayout.File || layout == dnlib.PE.ImageLayout.Memory;
	}

	/// <summary>
	/// Safe range check
	/// </summary>
	public static bool IsInRange(int value, int min, int max) {
		return value >= min && value <= max;
	}

	/// <summary>
	/// Safe range check (generic)
	/// </summary>
	public static bool IsInRange<T>(T value, T min, T max) where T : IComparable<T> {
		return value.CompareTo(min) >= 0 && value.CompareTo(max) <= 0;
	}

	/// <summary>
	/// Validate collection size
	/// </summary>
	public static bool IsValidCollectionSize<T>(System.Collections.Generic.ICollection<T>? collection, int minSize = 0, int maxSize = int.MaxValue) {
		if (collection == null)
			return minSize == 0;

		return collection.Count >= minSize && collection.Count <= maxSize;
	}
}