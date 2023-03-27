#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using Debug = System.Diagnostics.Debug;

namespace anatawa12.CallNativeDirectTest
{
    public class TestWindow : EditorWindow
    {
        [MenuItem("anatawa12/CallNativeDirectTest")]
        private static void Open() => CreateWindow<TestWindow>();

        private void OnGUI()
        {
            if (GUILayout.Button("Call Direct Binding"))
                EditorUtility.DisplayDialog("Hey!", "Calling Direct Binding Method!", "OK");
            if (GUILayout.Button("Call Original"))
                CallOriginal("Hey!", "Calling Original Method!", "OK");
        }

        private static bool CallOriginal(string title, string message, string ok)
        {
            // UnityEditor::UnityEditor.EditorUtility.DisplayDialog(title, message, ok, cancel);
            return (bool)GetOriginalMethod().Invoke(null, new object[] { title, message, ok });
        }

        private static MethodInfo GetOriginalMethod()
        {
            //var type = typeof(UnityEditor::UnityEditor.EditorUtility);
            var type = typeof(UnityEditor.Editor).Assembly.GetType("it");
            return type.GetMethod("DisplayDialog",
                new[] { typeof(string), typeof(string), typeof(string) });
        }
    }
    
    [InitializeOnLoad]
    public static class Patcher
    {
        static Patcher()
        {
            Patch();
        }

        private static MethodInfo GetOriginalDisplayDialog(Type[] args)
        {
            //var type = typeof(UnityEditor::UnityEditor.EditorUtility);
            var type = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.EditorUtility");
            return type.GetMethod("DisplayDialog", BindingFlags.Static | BindingFlags.Public | BindingFlags.Static,
                null, args, null);
        }

        private static MethodInfo GetWrapperDisplayDialog(Type[] args)
        {
            return typeof(Patcher).GetMethod("Wrapper", BindingFlags.Static | BindingFlags.NonPublic,
                null, args, null);
        }

        private static void Patch()
        {
            MonoModArchFixer.FixCurrentPlatform();

            var withCancel = new[] { typeof(string), typeof(string), typeof(string), typeof(string) };
            var noCancel = new[] { typeof(string), typeof(string), typeof(string) };

            Memory.DetourMethod(GetOriginalDisplayDialog(withCancel), GetWrapperDisplayDialog(withCancel));
            Memory.DetourMethod(GetOriginalDisplayDialog(noCancel), GetWrapperDisplayDialog(noCancel));
        }

        [UsedImplicitly]
        private static bool Wrapper(string title, string message, string ok, string cancel)
        {
            if (title.Equals("VRChat SDK", StringComparison.OrdinalIgnoreCase)) return true;

            return EditorUtility.DisplayDialog(title, message, ok, cancel);
        }

        [UsedImplicitly]
        private static bool Wrapper(string title, string message, string ok)
        {
            return Wrapper(title, message, ok, "");
        }
    }

    // MonoMod doesn't handle Rosetta environment correctly so fix that here.
    static class MonoModArchFixer
    {
        public static void FixCurrentPlatform()
        {
            var asm = typeof(Harmony).Assembly;
            var platformHelperType = asm.GetType("MonoMod.Utils.PlatformHelper");
            var platformType = asm.GetType("MonoMod.Utils.Platform");
            var currentField = platformHelperType.GetField("_current", BindingFlags.Static | BindingFlags.NonPublic);
            var currentLockedField = platformHelperType.GetField("_currentLocked", BindingFlags.Static | BindingFlags.NonPublic);
            var determinePlatformMethod = platformHelperType.GetMethod("DeterminePlatform", BindingFlags.Static | BindingFlags.NonPublic);

            Debug.Assert(determinePlatformMethod != null, nameof(determinePlatformMethod) + " != null");
            Debug.Assert(currentField != null, nameof(currentField) + " != null");
            Debug.Assert(currentLockedField != null, nameof(currentLockedField) + " != null");

            var locked = (bool)currentLockedField.GetValue(null);

            if (locked)
            {
                // verification only
                var currentPlatform = (Platform)Convert.ToInt32(currentField.GetValue(null));
                if (currentPlatform == Platform.Unknown)
                    return;
                var isCurrentArm = (currentPlatform & Platform.ARM) == Platform.ARM;
                Debug.Assert(isCurrentArm == IsARM, 
                    "locked detected platform for MonoMod and actual platform mismatch about ARM. " +
                    $"locked detected platform: isArm = {isCurrentArm}, actual: isArm = {IsARM}");
            }
            else
            {
                var currentPlatform = (Platform)Convert.ToInt32(currentField.GetValue(null));
                if (currentPlatform == Platform.Unknown)
                {
                    determinePlatformMethod.Invoke(null, Array.Empty<object>());
                    currentPlatform = (Platform)Convert.ToInt32(currentField.GetValue(null));
                }

                var isCurrentArm = (currentPlatform & Platform.ARM) == Platform.ARM;

                if (isCurrentArm != IsARM)
                {
                    if (IsARM)
                        currentPlatform |= Platform.ARM;
                    else
                        currentPlatform &= ~Platform.ARM;

                    currentField.SetValue(null, Enum.ToObject(platformType, currentPlatform));
                }
            }
        }

        private static bool IsARM => RuntimeInformation.ProcessArchitecture == Architecture.Arm ||
                              RuntimeInformation.ProcessArchitecture == Architecture.Arm64;
        
        // ReSharper disable InconsistentNaming
        // ReSharper disable UnusedMember.Local
        [Flags]
        enum Platform
        {
            OS = 1,
            Bits64 = 2,
            NT = 4,
            Unix = 8,
            ARM = 65536, // 0x00010000
            Wine = 131072, // 0x00020000
            Unknown = 17, // 0x00000011
            Windows = 37, // 0x00000025
            MacOS = 73, // 0x00000049
            Linux = 137, // 0x00000089
            Android = 393, // 0x00000189
            iOS = 585, // 0x00000249
        }
        // ReSharper restore UnusedMember.Local
        // ReSharper restore InconsistentNaming
    }
}

namespace UnityEditor
{ 
    static class EditorUtility
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern bool DisplayDialog(
            string title,
            string message,
            string ok,
            string cancel = "");
    }
}
#endif
