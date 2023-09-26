using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.Collections;
#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif
using System.IO;

namespace ClocknestGames.Library.Editor
{
	public class CGPostBuild
	{

#if UNITY_CLOUD_BUILD
		// This method is added in the Advanced Features Settings on UCB
		// PostBuildProcessor.OnPostprocessBuildiOS
		public static void OnPostprocessBuildiOS (string exportPath)
		{
			Debug.Log("[UCB] OnPostprocessBuildiOS");
			ProcessPostBuild(BuildTarget.iPhone,exportPath);
		}
#endif

		[PostProcessBuild]
		public static void OnPostprocessBuild(BuildTarget buildTarget, string path)
		{
#if !UNITY_CLOUD_BUILD
			Debug.Log("[iOS] OnPostprocessBuild");
			ProcessPostBuild(buildTarget, path);
#endif
		}

		public static void ProcessPostBuild(BuildTarget buildTarget, string path)
		{
#if UNITY_IOS
			if (buildTarget == BuildTarget.iOS)
			{

				Debug.Log("[iOS] OnPostprocessBuild - PList");

				// Get plist
				string plistPath = path + "/Info.plist";
				PlistDocument plist = new PlistDocument();
				plist.ReadFromString(File.ReadAllText(plistPath));

				// Get root
				PlistElementDict rootDict = plist.root;
				rootDict.SetBoolean("ITSAppUsesNonExemptEncryption", false);

				// Write to file
				File.WriteAllText(plistPath, plist.WriteToString());
			}
#endif
		}
	}
}