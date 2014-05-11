﻿using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;


[CustomEditor(typeof(BackupProject))]
public class BackupProjectEditor : Editor  
{
	BackupProject backupScript;
	string[] assetsName = new string[]{"Shaders", "Compute Shader", "Fbx", "All"};
	int[] assets = new int[]{0, 1, 2, 3};
	
	string[] timeNames = new string[] {"10 Min", "15 Min", "30 Min", "45 Min", "1Hr", "2Hr", "4Hr", "8Hr"};
	int[] times = new int[]{10, 15, 30, 45, 60, 120, 240, 480};
	
	object Init ()
	{
		BackupProject backupProj = (BackupProject)target;
		return backupProj;
	}

	public override void OnInspectorGUI ()
	{
		Skin();
		backupScript = Init() as BackupProject;
		string projectPath = Application.dataPath;
		projectPath = projectPath.Substring(0, projectPath.Length - 7);
		backupScript.sourceDirectory = projectPath;
		EditorGUILayout.LabelField("Source", backupScript.sourceDirectory);
		GUILayout.BeginHorizontal();
		if(EditorApplication.isPlayingOrWillChangePlaymode)
			GUI.enabled = false;
		else
			GUI.enabled = backupScript.manualBackup;
		backupScript.destinationDirectory = EditorGUILayout.TextField("Destination", backupScript.destinationDirectory,GUILayout.MaxWidth(1000));
		if(GUILayout.Button("Edit", GUILayout.MaxWidth(50),GUILayout.MaxHeight(20)))
			backupScript.destinationDirectory = EditorUtility.OpenFolderPanel("Select destination directory","","");
		GUILayout.EndHorizontal();
		if(System.String.IsNullOrEmpty(backupScript.destinationDirectory))
			EditorGUILayout.HelpBox("Please select destination folder", MessageType.Warning);
		backupScript.selectedSaveAssets = EditorGUILayout.IntPopup("Save",backupScript.selectedSaveAssets,assetsName,assets);
		if(EditorApplication.isPlayingOrWillChangePlaymode)
			GUI.enabled = false;
		else
		GUI.enabled = true;
		backupScript.assetsTypes = (BackupProject.AssetsTypes)(backupScript.selectedSaveAssets);

		backupScript.autoBackup = EditorGUILayout.ToggleLeft("AutoBackup", backupScript.autoBackup);
		if (backupScript.autoBackup)
		{
			backupScript.manualBackup = false;
			//select backup time interval
			backupScript.selectedTime = EditorGUILayout.IntPopup("Interval",backupScript.selectedTime,timeNames,times);
			backupScript.backupTime = convertMinToSec(backupScript.selectedTime);
			if(EditorApplication.isPlayingOrWillChangePlaymode)
				EditorGUILayout.HelpBox("Backup disabled in PlayMode", MessageType.Info);
			else
				EditorGUILayout.LabelField("Time to backup", (Mathf.RoundToInt((float)(backupScript.nextBackup-EditorApplication.timeSinceStartup))).ToString()+" sec");
			EditorApplication.update = EditorUpdate;
		}
		else 
		{
			backupScript.manualBackup = true;
			EditorApplication.update = null;
			backupScript.nextBackup = 0;
		}
		

		
		
		//center element
		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		if(EditorApplication.isPlayingOrWillChangePlaymode)
			GUI.enabled = false;
		else
		GUI.enabled = backupScript.manualBackup;
		if(GUILayout.Button("Backup", GUILayout.MaxWidth(100),GUILayout.MaxHeight(30)))
		{
			Backup(backupScript.sourceDirectory, backupScript.destinationDirectory);
			
		}

		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
		//refresh autoBackup time if select new interval
		if(GUI.changed && backupScript.autoBackup)
		{
			backupScript.nextBackup = 0;
		}
	}
	
	void EditorUpdate ()
	{
		BackupByTime();
	}
	
	
	double convertMinToSec (int minutes)
	{
		return (double)(minutes*60);
	}
	void BackupByTime ()
	{
		if(backupScript.nextBackup <= 0)
			backupScript.nextBackup = EditorApplication.timeSinceStartup + backupScript.backupTime;
		if(EditorApplication.timeSinceStartup > backupScript.nextBackup)
		{
			if(!EditorApplication.isPlayingOrWillChangePlaymode)
			{
				Backup(backupScript.sourceDirectory, backupScript.destinationDirectory);
			}
			backupScript.nextBackup=EditorApplication.timeSinceStartup + backupScript.backupTime;
		}
		Repaint();
	}
	
	
	void OnInspectorUpdate()
	{
		Repaint();
	}
	
	void Skin ()
	{
		GUI.backgroundColor = Color.cyan;
		GUI.contentColor = Color.yellow;
	}
	
	
	void ProgressBar (float value, string text)
	{
		GUIStyle style = new GUIStyle();
		GUI.backgroundColor = Color.cyan;
		GUI.contentColor = Color.yellow;
		
		Rect rect = GUILayoutUtility.GetRect(25, 25, style);
		EditorGUI.ProgressBar(rect, value, text);
		EditorGUILayout.Space();
		
	}
	
	private float progress = 0.0f;
	private float allProgress = 0.0f;
	private void Backup(string sourcePath, string destionationPath)
	{
		progress = 0.0f;
		allProgress = 0.0f;
		string extensions="";
		switch (backupScript.assetsTypes)
		{

		case BackupProject.AssetsTypes.Shader:
			extensions="*shader";
			break;
		case BackupProject.AssetsTypes.ComputeShader:
			extensions="*compute";
			break;
		case BackupProject.AssetsTypes.FBX:
			extensions="*fbx";
			break;
		case BackupProject.AssetsTypes.All:
			extensions="*";
			break;
		}

		//save current scene
		EditorApplication.SaveScene();

		//calculate progress
		allProgress = Directory.GetFiles(sourcePath, extensions, SearchOption.AllDirectories).Length;
		if (allProgress == 0.0f)
			Debug.Log("Project not include " + extensions + " files");
		else
		{
		//create folders
		foreach (string directoryPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
		{
			string folderName = directoryPath.Substring(directoryPath.Length - 4, 4);
			if (folderName != "Temp")
				Directory.CreateDirectory(directoryPath.Replace(sourcePath,destionationPath));
		}

		float onePercent = 100.0f/allProgress;
		//copy files
		foreach (string filesPath in Directory.GetFiles(sourcePath, extensions, SearchOption.AllDirectories))
		{
			if (!filesPath.Contains("Temp"))
			{
				File.Copy(filesPath, filesPath.Replace(sourcePath, destionationPath), true); //true - available overwrite files
				progress+=onePercent;
				EditorUtility.DisplayProgressBar("Backup",Mathf.RoundToInt(progress).ToString()+"%",progress/100.0f);
			}
		}
		//set files attributes
		foreach (string destinationFile  in Directory.GetFiles(destionationPath, extensions, SearchOption.AllDirectories))
		{
			if (Path.GetExtension(destinationFile) != ".meta")
				File.SetAttributes(destinationFile, FileAttributes.Normal);
		}
		EditorUtility.ClearProgressBar();
		Debug.Log("Backup created");
		}
	}
}