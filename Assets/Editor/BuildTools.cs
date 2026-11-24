using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildTools
{
  [MenuItem("Go Heroes/Build WebGL")]
  public static void BuildWebGL()
  {
    // itch.io serves the files without Content-Encoding headers, so the gzip build
    // needs the JS decompression fallback (.unityweb files).
    PlayerSettings.companyName = "Stacktone";
    PlayerSettings.productName = "Go Heroes";
    PlayerSettings.WebGL.template = "PROJECT:FillWindow";
    PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
    PlayerSettings.WebGL.decompressionFallback = true;
    PlayerSettings.WebGL.dataCaching = true;

    Build(BuildTarget.WebGL, "Builds/WebGL");
  }

  static void Build(BuildTarget target, string path)
  {
    Directory.CreateDirectory(path);
    var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
    {
      scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
      locationPathName = path,
      target = target,
      options = BuildOptions.None,
    });
    Debug.Log($"Build {report.summary.result}: {report.summary.totalSize / (1024 * 1024)} MB in {report.summary.totalTime}");
    if (report.summary.result != BuildResult.Succeeded && Application.isBatchMode) EditorApplication.Exit(1);
  }
}
