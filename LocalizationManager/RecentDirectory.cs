using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace LocalizationManager
{
    public class RecentDirectory
	{
		private string saveKey = "recentDirectoryList";

		private static RecentDirectory instance = null;
		private List<string> recentDirs = new List<string>();
	    public int ListLength { get; set; }

		public static RecentDirectory Instance
		{
			get
			{
				if (instance == null)
				{
					instance = new RecentDirectory();
				}
				return instance;
			}
		}

		private RecentDirectory() : this(5)
	    {           
	    }
	
	    private RecentDirectory(int listLength)
	    {
			ListLength = listLength;

			var dirs = RegistryManager.Instance.LoadMultiStr(saveKey);
			if (dirs != null)
			{
				recentDirs = dirs;
			}
	    }
	
	    public void AddRecentDir(string recentDir)
	    {
	        recentDirs.Remove(recentDir);
	        recentDirs.Add(recentDir);
	        if (recentDirs.Count > ListLength)
	            recentDirs.RemoveAt(0);

			RegistryManager.Instance.StoreMultiStr(saveKey, recentDirs.ToArray());
	    }
	
	    public void RemoveDir(string directoryName)
	    {
	        recentDirs.Remove(directoryName);
			RegistryManager.Instance.StoreMultiStr(saveKey, recentDirs.ToArray());
		}
	
	    public List<string> GetRecentDirs()
	    {
			List<string> dirIndexList = new List<string>();

			foreach (string dir in recentDirs)
			{
				if (Directory.Exists(dir) == true)
				{
					dirIndexList.Add(dir);
				}
			}

			recentDirs = dirIndexList;
			RegistryManager.Instance.StoreMultiStr(saveKey, recentDirs.ToArray());

			return recentDirs;
	    }
	
	}
}
