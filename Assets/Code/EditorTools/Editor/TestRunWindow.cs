using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class TestRunWindow : EditorWindow
{
    [MenuItem("RtcLib/Windows/Test Tools")]
    public static void ShowWindow()
    {
        GetWindow<TestRunWindow>(false, "Project Tools", true);
    }

    static void StopAll()
    {
        KillAllProcesses();
        EditorApplication.isPlaying = false;
    }

    static void KillAllProcesses()
    {
        var buildExe = GetBuildExe(EditorUserBuildSettings.activeBuildTarget);

        var processName = Path.GetFileNameWithoutExtension(buildExe);
        var processes = System.Diagnostics.Process.GetProcesses();
        foreach (var process in processes)
        {
            if (process.HasExited)
                continue;

            try
            {
                if (process.ProcessName != null && process.ProcessName == processName)
                {
                    process.Kill();
                }
            }
            catch (InvalidOperationException)
            {

            }
        }
    }

    static string GetBuildPath(BuildTarget buildTarget)
    {
        if (buildTarget == BuildTarget.PS4)
            return "AutoBuildPS4";
        else
            return "AutoBuild";
    }

    static string GetBuildExeName(BuildTarget buildTarget)
    {
        if (buildTarget == BuildTarget.PS4)
            return "AutoBuild";
        else
            return "AutoBuild.exe";
    }

    static string GetBuildExe(BuildTarget buildTarget)
    {
        if (buildTarget == BuildTarget.PS4)
            return "AutoBuild/AutoBuild.bat";
        else
            return "AutoBuild.exe";
    }

    void OnGUI()
    {
        DrawTestTools();
    }

    void DrawTestTools()
    {
        //draw the buttons to build and launch
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Build"))
        {
            var buildOptions = BuildOptions.AllowDebugging;
                buildOptions |= BuildOptions.Development;

            //BuildTools.BuildGame(GetBuildPath(buildTarget), GetBuildExeName(buildTarget), buildTarget, buildOptions, "AutoBuild", m_IL2CPP);

           // if (action == BuildAction.BuildAndRun)
           //     RunBuild("");
            GUIUtility.ExitGUI(); // prevent warnings from gui about unmatched layouts
        }
        if (GUILayout.Button("Test"))
        {

        }
        GUILayout.EndHorizontal();
    }


}