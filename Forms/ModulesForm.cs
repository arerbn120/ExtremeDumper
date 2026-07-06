using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ExtremeDumper.Diagnostics;
using ExtremeDumper.Dumping;
using ExtremeDumper.Logging;
using ImageLayout = dnlib.PE.ImageLayout;

namespace ExtremeDumper.Forms;

partial class ModulesForm : Form {
	static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();

	readonly ProcessInfo process;
	readonly StrongBox<DumperType> dumperType;
	readonly TitleComposer title;
	readonly List<ModuleInfo> modules = new();

	public ModulesForm(ProcessInfo process, StrongBox<DumperType> dumperType) {
		InitializeComponent();
		Utils.ScaleByDpi(this);
		this.process = process;
		this.dumperType = dumperType;
		title = new TitleComposer {
			Title = "Modules",
			Subtitle = process.Name
		};
		title.Annotations["PID"] = $"PID={process.Id}";
		Text = title.Compose(true);
		Utils.EnableDoubleBuffer(lvwModules);
		lvwModules.ListViewItemSorter = new ListViewItemSorter(lvwModules, new[] { TypeCode.String, TypeCode.String, TypeCode.String, TypeCode.UInt64, TypeCode.Int32, TypeCode.String }) { AllowHexLeading = true };
		RefreshModuleList();
	}

	#region Events
	void lvwModules_Resize(object sender, EventArgs e) {
		lvwModules.AutoResizeColumns(true);
	}

	async void mnuDumpModule_Click(object sender, EventArgs e) {
		if (!TryGetSelectedModule(out var module))
			return;

		try {
			mnuDumpModule.Enabled = false;
			title.Annotations["DUMP"] = "Dumping";
			Text = title.Compose(true);

			string filePath = EnsureValidFileName(module.Name);
			if (filePath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) || filePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
				filePath = PathInsertPostfix(filePath, ".dump");
			else
				filePath += ".dump.dll";
			sfdlgDumped.FileName = filePath;
			sfdlgDumped.InitialDirectory = Path.GetDirectoryName(process.FilePath);
			if (sfdlgDumped.ShowDialog() != DialogResult.OK)
				return;

			var imageLayout = module is DotNetModuleInfo dnModule && dnModule.InMemory ? ImageLayout.File : ImageLayout.Memory;
			bool result = await Task.Run(() => DumpModule(module.ImageBase, imageLayout, sfdlgDumped.FileName));
			if (result)
				Logger.Info($"Dump module successfully. Image was saved to: {sfdlgDumped.FileName}");
			else
				Logger.Error("Fail to dump module.");
		}
		catch (Exception ex) {
			Logger.Error("Exception occurred while dumping module");
			Logger.Exception(ex);
		}
		finally {
			title.Annotations["DUMP"] = null;
			Text = title.Compose(true);
			mnuDumpModule.Enabled = true;
		}
	}

	async void mnuRefreshModuleList_Click(object sender, EventArgs e) {
		try {
			mnuRefreshModuleList.Enabled = false;
			mnuOnlyDotNetModule.Enabled = false;
			title.Annotations["REFRESH"] = "Refreshing";
			Text = title.Compose(true);
			if (mnuEnableAntiAntiDump.Checked)
				await RefreshModuleListAAD();
			else
				RefreshModuleList();
		}
		finally {
			title.Annotations["REFRESH"] = null;
			Text = title.Compose(true);
			mnuRefreshModuleList.Enabled = true;
			mnuOnlyDotNetModule.Enabled = true;
		}
	}

	void mnuOnlyDotNetModule_Click(object sender, EventArgs e) {
		RefreshModuleList();
	}

	void mnuEnableAntiAntiDump_Click(object sender, EventArgs e) {
		// UI feedback for advanced dumping
		if (mnuEnableAntiAntiDump.Checked) {
			title.Annotations["MODE"] = "Advanced";
		} else {
			title.Annotations["MODE"] = null;
		}
		Text = title.Compose(true);
	}

	void mnuCopyAddress_Click(object sender, EventArgs e) {
		if (TryGetSelectedModule(out var module))
			Clipboard.SetText(Formatter.FormatHex(module.ImageBase));
	}

	void mnuCopyName_Click(object sender, EventArgs e) {
		if (TryGetSelectedModule(out var module))
			Clipboard.SetText(module.Name);
	}
	#endregion

	bool TryGetSelectedModule([NotNullWhen(true)] out ModuleInfo? module) {
		module = null;
		if (lvwModules.SelectedIndices.Count == 0)
			return false;

		nuint moduleHandle = nuint.Parse(lvwModules.GetFirstSelectedSubItem(chImageBase.Index).Text, NumberStyles.AllowHexSpecifier);
		module = modules.Find(t => t.ImageBase == moduleHandle);
		Debug2.Assert(module is not null);
		return true;
	}

	void RefreshModuleList() {
		modules.Clear();
		if (process is not DotNetProcessInfo dotNetProcess) {
			lvwModules.Items.Clear();
			return;
		}

		foreach (var module in dotNetProcess.Modules.Where(t => !mnuOnlyDotNetModule.Checked || t is DotNetModuleInfo)) {
			modules.Add(module);
		}

		Utils.RefreshListView(lvwModules, modules, CreateListViewItem, 10);
	}

	async Task RefreshModuleListAAD() {
		modules.Clear();
		lvwModules.Items.Clear();

		try {
			await Task.Run(() => {
				var aadClient = AADCoreInjector.Inject(process.Id, InjectionClrVersion.V4);
				try {
					if (!aadClient.EnableMultiDomain(out var clients))
						return;

					foreach (var client2 in clients) {
						if (!client2.GetModules(out var modules2))
							continue;

						foreach (var module in modules2.Values) {
							var moduleInfo = new DotNetModuleInfo(module.Name, (nuint)module.ImageBase, (uint)module.ImageSize, module.FilePath, string.Empty, string.Empty);
							modules.Add(moduleInfo);
						}
					}
				}
				finally {
					aadClient.Disconnect();
				}
			});

			Utils.RefreshListView(lvwModules, modules, CreateListViewItem, 10);
		}
		catch (Exception ex) {
			Logger.Error("AntiAntiDump failed");
			Logger.Exception(ex);
		}
	}

	static ListViewItem CreateListViewItem(ModuleInfo module) {
		var item = new ListViewItem(module.Name);
		item.SubItems.Add(Formatter.FormatHex(module.ImageBase));
		item.SubItems.Add(Formatter.FormatSize(module.ImageSize));
		item.SubItems.Add(module.FilePath);

		if (module is DotNetModuleInfo dnModule) {
			item.SubItems.Add(dnModule.DomainName);
			item.SubItems.Add(dnModule.ClrVersion);
			item.BackColor = Utils.DotNetColor;
		}
		else {
			item.SubItems.Add(string.Empty);
			item.SubItems.Add(string.Empty);
		}

		return item;
	}

	static string EnsureValidFileName(string fileName) {
		if (string.IsNullOrEmpty(fileName))
			return string.Empty;

		var newFileName = new StringBuilder(fileName.Length);
		foreach (char chr in fileName) {
			if (!InvalidFileNameChars.Contains(chr))
				newFileName.Append(chr);
		}
		return newFileName.ToString();
	}

	bool DumpModule(nuint moduleHandle, ImageLayout imageLayout, string filePath) {
		using var dumper = DumperFactory.Create(process.Id, GetDumperType());
		return dumper.DumpModule(moduleHandle, imageLayout, filePath);
	}

	DumperType GetDumperType() {
		if (mnuEnableAntiAntiDump.Checked)
			return DumperType.AdvancedAntiAntiDump;
		return dumperType.Value;
	}

	static string PathInsertPostfix(string path, string postfix) {
		return Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path) + postfix + Path.GetExtension(path));
	}
}