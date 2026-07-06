using System;

namespace ExtremeDumper.Dumping;

public enum DumperType {
	Normal = 0,
	AntiAntiDump = 1,
	AdvancedAntiAntiDump = 2  // NEW: Advanced with Reactor bypass
}

public static class DumperFactory {
	public static IDumper Create(uint processId, DumperType dumperType) {
		switch (dumperType) {
		case DumperType.Normal:
			return new NormalDumper(processId);
		case DumperType.AntiAntiDump:
			return new AntiAntiDumper(processId);
		case DumperType.AdvancedAntiAntiDump:
			return new AdvancedAntiAntiDumper(processId);
		default:
			throw new ArgumentOutOfRangeException(nameof(dumperType));
		}
	}
}