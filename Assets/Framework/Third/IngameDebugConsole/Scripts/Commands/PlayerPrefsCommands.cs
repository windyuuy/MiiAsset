using UnityEngine;

namespace IngameDebugConsole.Commands
{
	public class PlayerPrefsCommands
	{
		[ConsoleMethod( "prefs.int", "Returns the value of an Integer PlayerPrefs field" ), UnityEngine.Scripting.Preserve]
		public static string PlayerPrefsGetInt( string key )
		{
			if( !UnityEngine.PlayerPrefs.HasKey( key ) ) return "Key Not Found";
			return UnityEngine.PlayerPrefs.GetInt( key ).ToString();
		}

		[ConsoleMethod( "prefs.int", "Sets the value of an Integer PlayerPrefs field" ), UnityEngine.Scripting.Preserve]
		public static void PlayerPrefsSetInt( string key, int value )
		{
			UnityEngine.PlayerPrefs.SetInt( key, value );
		}

		[ConsoleMethod( "prefs.float", "Returns the value of a Float PlayerPrefs field" ), UnityEngine.Scripting.Preserve]
		public static string PlayerPrefsGetFloat( string key )
		{
			if( !UnityEngine.PlayerPrefs.HasKey( key ) ) return "Key Not Found";
			return UnityEngine.PlayerPrefs.GetFloat( key ).ToString();
		}

		[ConsoleMethod( "prefs.float", "Sets the value of a Float PlayerPrefs field" ), UnityEngine.Scripting.Preserve]
		public static void PlayerPrefsSetFloat( string key, float value )
		{
			UnityEngine.PlayerPrefs.SetFloat( key, value );
		}

		[ConsoleMethod( "prefs.string", "Returns the value of a String PlayerPrefs field" ), UnityEngine.Scripting.Preserve]
		public static string PlayerPrefsGetString( string key )
		{
			if( !UnityEngine.PlayerPrefs.HasKey( key ) ) return "Key Not Found";
			return UnityEngine.PlayerPrefs.GetString( key );
		}

		[ConsoleMethod( "prefs.string", "Sets the value of a String PlayerPrefs field" ), UnityEngine.Scripting.Preserve]
		public static void PlayerPrefsSetString( string key, string value )
		{
			UnityEngine.PlayerPrefs.SetString( key, value );
		}

		[ConsoleMethod( "prefs.delete", "Deletes a PlayerPrefs field" ), UnityEngine.Scripting.Preserve]
		public static void PlayerPrefsDelete( string key )
		{
			UnityEngine.PlayerPrefs.DeleteKey( key );
		}

		[ConsoleMethod( "prefs.clear", "Deletes all PlayerPrefs fields" ), UnityEngine.Scripting.Preserve]
		public static void PlayerPrefsClear()
		{
			UnityEngine.PlayerPrefs.DeleteAll();
		}
	}
}