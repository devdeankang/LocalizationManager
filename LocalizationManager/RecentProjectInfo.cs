using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace LocalizationManager
{
	public class ProjectInfo
	{
		public string Name { get; set; }
		public string Directory { get; set; }

		public ProjectInfo(string name, string dir)
		{
			Name = name;
			Directory = dir;
		}
	}

	public class RecentProjectInfo
	{
		private static RecentProjectInfo instance = null;
		private List<ProjectInfo> recentProjectInfo = new List<ProjectInfo>();

		private string RegistryKey = "Software\\LocalizationManager";
		private string nameKey = "recentProjectNameList";
		private string dirKey = "recentProjectDirList";

		public int ListLength { get; set; }

		public static RecentProjectInfo Instance
		{
			get
			{
				if (instance == null)
				{
					instance = new RecentProjectInfo();
				}
				return instance;
			}
		}

		private RecentProjectInfo() : this(5)
		{
		}

		private RecentProjectInfo(int listLength)
		{
			ListLength = listLength;
			Load();
		}

		public void AddRecentFile(ProjectInfo info)
		{
			RemoveFile(info);

			recentProjectInfo.Add(info);
			if (recentProjectInfo.Count > ListLength)
				recentProjectInfo.RemoveAt(0);

			Store();
		}

		public void RemoveFile(ProjectInfo info)
		{
			for (int i = 0; i < recentProjectInfo.Count; i++)
			{
				if (recentProjectInfo[i].Name.Equals(info.Name))
				{
					if (recentProjectInfo[i].Directory.Equals(info.Directory))
					{
						recentProjectInfo.RemoveAt(i);
						break;
					}
				}
			}
			
			Store();
		}

		public List<ProjectInfo> GetRecentFiles()
		{
			return recentProjectInfo;
		}

		private void Store()
		{
			RegistryKey k = Registry.CurrentUser.OpenSubKey(RegistryKey);
			if (k == null)
				k = Registry.CurrentUser.CreateSubKey(RegistryKey);
			k = Registry.CurrentUser.OpenSubKey(RegistryKey, true);

			k.SetValue(nameKey, recentProjectNameList().ToArray());
			k.SetValue(dirKey, recentProjectDirList().ToArray());
		}

		private void Load()
		{
			RegistryKey k = Registry.CurrentUser.OpenSubKey(RegistryKey);
			if (k == null)
				k = Registry.CurrentUser.CreateSubKey(RegistryKey);

			if (k.GetValue(nameKey) != null && k.GetValueKind(nameKey) == RegistryValueKind.MultiString &&
				k.GetValue(dirKey) != null && k.GetValueKind(dirKey) == RegistryValueKind.MultiString)
			{
				string[] nameList = (string[])(k.GetValue(nameKey));
				string[] dirList = (string[])(k.GetValue(dirKey));

				for (int i = 0; i < nameList.Length; i++)
				{
					if (Directory.Exists(dirList[i]) 
						&& File.Exists(Path.Combine(dirList[i], string.Format("{0}.lmp", nameList[i]))))
					{
						ProjectInfo projectInfo = new ProjectInfo(nameList[i], dirList[i]);

						recentProjectInfo.Add(projectInfo);
					}
				}
			}

			Store();
		}

		public List<string> recentProjectNameList()
		{
			List<string> nameArr = new List<string>();

			for (int i = 0; i < recentProjectInfo.Count; i++)
			{
				nameArr.Add(recentProjectInfo[i].Name);
			}

			return nameArr;
		}

		public List<string> recentProjectDirList()
		{
			List<string> dirArr = new List<string>();

			for (int i = 0; i < recentProjectInfo.Count; i++)
			{
				dirArr.Add(recentProjectInfo[i].Directory);
			}

			return dirArr;
		}
	}
}
