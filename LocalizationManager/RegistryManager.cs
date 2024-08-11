using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace LocalizationManager
{
	class RegistryManager
	{
		public string REGISTRY_KEY_STARTS = "Software\\LocalizationManager";
		private string registryKey = string.Empty;

		private static RegistryManager instance = null;

		private RegistryManager()
		{
			SetRegistryKey();
		}

		private void SetRegistryKey()
		{
			if (LocalizationDataManager.Instance.configData == null)
				return;

			registryKey = string.Format("{0}\\{1}", REGISTRY_KEY_STARTS, LocalizationDataManager.Instance.configData.ProjectName);
		}

		public static RegistryManager Instance
		{
			get
			{
				if (instance == null)
				{
					instance = new RegistryManager();
				}

				if (string.IsNullOrEmpty(instance.registryKey) == true)
					instance.SetRegistryKey();

				return instance;
			}
		}

		public void StoreStr(string key, string value, string regeditKey)
		{
			RegistryKey k = Registry.CurrentUser.OpenSubKey(regeditKey);
			if (k == null)
				k = Registry.CurrentUser.CreateSubKey(regeditKey);
			k = Registry.CurrentUser.OpenSubKey(regeditKey, true);

			k.SetValue(key, value);
		}

		public void StoreStr(string key, string value)
		{
			StoreStr(key, value, registryKey);
		}

		public void StoreMultiStr(string key, string[] value)
		{
			RegistryKey k = Registry.CurrentUser.OpenSubKey(registryKey);
			if (k == null)
				k = Registry.CurrentUser.CreateSubKey(registryKey);
			k = Registry.CurrentUser.OpenSubKey(registryKey, true);

			k.SetValue(key, value);
		}

		public string LoadStr(string key, string regeditKey)
		{
			RegistryKey k = Registry.CurrentUser.OpenSubKey(regeditKey);
			if (k == null)
				k = Registry.CurrentUser.CreateSubKey(regeditKey);

			if (k.GetValue(key) != null && k.GetValueKind(key) == RegistryValueKind.String)
			{
				return (string)k.GetValue(key, string.Empty);
			}

			return string.Empty;

		}

		public string LoadStr(string key)
		{
			return LoadStr(key, registryKey);
		}

		public List<string> LoadMultiStr(string key)
		{
			RegistryKey k = Registry.CurrentUser.OpenSubKey(registryKey);
			if (k == null)
				k = Registry.CurrentUser.CreateSubKey(registryKey);

			if (k.GetValue(key) != null && k.GetValueKind(key) == RegistryValueKind.MultiString)
			{
				string[] list = (string[])(k.GetValue(key));
				return new List<string>(list);
			}

			return null;
		}
	}
}
