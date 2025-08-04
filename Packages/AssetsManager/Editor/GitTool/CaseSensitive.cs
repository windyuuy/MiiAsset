using UnityEditor;
using System.Diagnostics;
using System.Text;
using Debug = UnityEngine.Debug;
using System;
using UnityEngine;

[InitializeOnLoad]
public class CaseSensitive
{
    static string ProjectCUID;
    static CaseSensitive()
	{
        ProjectCUID=$"StartUp-CaseSensitive-{System.Environment.CurrentDirectory}";
		EditorApplication.quitting -= OnEditorQuit;
		EditorApplication.quitting += OnEditorQuit;

		if (!EditorPrefs.HasKey(ProjectCUID))
        {
            // 通过标记记录是否已经执行过该方法
            OnEditorStartUp();
            EditorPrefs.SetInt(ProjectCUID, 1);
        }
    }
	/// <summary>
	/// UnityEditor 关闭时取消标记
	/// </summary>
	private static void OnEditorQuit()
	{
		EditorPrefs.DeleteKey(ProjectCUID);
	}

	static void OnEditorStartUp() {
		var ret = false;

        Process p = new Process();
        //设置要启动的应用程序
        p.StartInfo.FileName = "cmd";
        p.StartInfo.Arguments = $"/c git config core.ignorecase false";
        p.StartInfo.WorkingDirectory = "./";
        //是否使用操作系统shell启动
        p.StartInfo.UseShellExecute = false;
        // 接受来自调用程序的输入信息
        p.StartInfo.RedirectStandardInput = true;
        //输出信息
        p.StartInfo.RedirectStandardOutput = true;
        // 输出错误
        p.StartInfo.RedirectStandardError = true;
        //不显示程序窗口
        p.StartInfo.CreateNoWindow = true;

		Debug.Log($"RunCmd: git config core.ignorecase false");

        if (EditorUtility.DisplayCancelableProgressBar("正在设置大小写敏感", "正在设置大小写敏感...", 0.2f))
        {
            EditorUtility.ClearProgressBar();
        }

        try
        {
            //启动程序
            p.Start();

            p.StandardInput.AutoFlush = true;

			//获取输出信息
			StringBuilder strErrorBuilder = new StringBuilder();//.StandardError.ReadToEnd();
            string strOuput = p.StandardOutput.ReadToEnd();

			p.ErrorDataReceived += (sender, evt) =>
			{
                if (evt.Data != null)
                {
                    strErrorBuilder.AppendLine(evt.Data);
                }
			};
			//等待程序执行完退出进程
			p.WaitForExit();

			p.Close();

            var strError=strErrorBuilder.ToString();
            if (!string.IsNullOrEmpty(strError))
            {
                Debug.Log(strOuput);
                Debug.LogError(strError);
                Debug.LogError("设置大小写敏感存在异常, 请检查日志");
            }
            else
            {
                Debug.Log(strOuput);
                Debug.Log("设置大小写敏感成功");
            }
            ret = true;

            if (EditorUtility.DisplayCancelableProgressBar("正在设置大小写敏感", "正在设置大小写敏感...", 1f))
            {
                ret = false;
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        if (ret)
        {
			Debug.Log("设置大小写敏感成功");
        }
        else
        {
			Debug.Log("设置大小写敏感失败");
		}
	}
}
