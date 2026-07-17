/*
 * Author: Trung Dong
 * www.trung-dong.com
 * Last update: 2018/01/21
 * 
 * This software is provided 'as-is', without any express or implied
 * warranty.  In no event will the authors be held liable for any damages
 * arising from the use of this software.
 * 
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 * 
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would be
 *    appreciated but is not required.
 * 2. Altered source versions must be plainly marked as such, and must not be
 *    misrepresented as being the original software.
 * 3. This notice may not be removed or altered from any source distribution.
*/
using UnityEngine;
using UnityEditor;

using System.Collections.Generic;
using System.Collections;

using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;

using Newtonsoft.Json;

using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System;
using System.Net;
using System.IO;
using System.Threading;
using System.Globalization;
using System.Text;

public class GoogleSheetsToFiles : EditorWindow
{
	private enum OutputDataType
    {
		Json,
		CSV,
		List
    }

	private static string CLIENT_ID = "871414866606-7b9687cp1ibjokihbbfl6nrjr94j14o8.apps.googleusercontent.com";
	private static string CLIENT_SECRET = "zF_J3qHpzX5e8i2V-ZEvOdGV";
	private static string[] Scopes = { SheetsService.Scope.SpreadsheetsReadonly };

	/// <summary>
	/// Name of application.
	/// </summary>
	private static string appName = "Unity";

	/// <summary>
	/// The root of spreadsheet's url.
	/// </summary>
	private static string urlRoot = "https://spreadsheets.google.com/feeds/spreadsheets/";

	/// <summary>
	/// The data types which is allowed to convert from sheet to json object
	/// </summary>
	private static List<string> allowedDataTypes = new List<string>(){"string", "int", "bool", "float", "string[]", "int[]", "bool[]", "float[]"};

	public static void DownloadCSVFiles(string spreadSheetKey, string pathToStoreFiles)
	{
		DownloadToFiles(OutputDataType.CSV, spreadSheetKey, pathToStoreFiles);
	}

	public static void DownloadJsonFiles(string spreadSheetKey, string pathToStoreFiles)
	{
		DownloadToFiles(OutputDataType.Json, spreadSheetKey, pathToStoreFiles);
	}

	public static void DownloadListFiles(string spreadSheetKey, string pathToStoreFiles)
	{
		DownloadToFiles(OutputDataType.List, spreadSheetKey, pathToStoreFiles);
	}

	private static void DownloadToFiles(OutputDataType outputDataType, string spreadSheetKey, string pathToStoreFiles)
	{
		ServicePointManager.ServerCertificateValidationCallback = MyRemoteCertificateValidationCallback;

		DirectoryInfo outputDirectoryInfo = new DirectoryInfo(Path.Combine(Application.dataPath, pathToStoreFiles));
		var outputDir = outputDirectoryInfo.FullName;
		//Validate input
		if (string.IsNullOrEmpty(spreadSheetKey))
		{
			Debug.LogError("spreadSheetKey can not be null!");
			return;
		}

		Debug.Log ("Start downloading from key: " + spreadSheetKey);

		var service = new SheetsService(new BaseClientService.Initializer()
			{
				HttpClientInitializer = GetCredential(),
				ApplicationName = appName,
			});

		Spreadsheet spreadSheetData = service.Spreadsheets.Get (spreadSheetKey).Execute ();
		IList<Sheet> sheets = spreadSheetData.Sheets;

		if ((sheets == null) || (sheets.Count <= 0))
		{
			Debug.LogError("Not found any data!");
			return;
		}

		//For each sheet in received data, check the sheet name. If that sheet is the wanted sheet, add it into the ranges.
		List<string> ranges = new List<string>();
		foreach (Sheet sheet in sheets)
		{
			if (sheet.Properties.Hidden != true)
			{
				ranges.Add(sheet.Properties.Title);
			}
		}
			
		SpreadsheetsResource.ValuesResource.BatchGetRequest request = service.Spreadsheets.Values.BatchGet(spreadSheetKey);
		request.Ranges = ranges;
		BatchGetValuesResponse response = request.Execute();

		//For each wanted sheet, create a json file
		foreach(ValueRange valueRange in response.ValueRanges)
		{
			string sheetname = valueRange.Range.Split('!')[0];
			// Remove apostrophes for compound names
			if (sheetname[0] == '\'')
			{
				sheetname = sheetname.Substring(1, sheetname.Length - 2);
			}
			if (outputDataType == OutputDataType.Json)
			{
				CreateJsonFile(sheetname, outputDir, valueRange);
			}
			else if (outputDataType == OutputDataType.CSV)
            {
				CreateCSVFile(sheetname, outputDir, valueRange);
			}
			else if (outputDataType == OutputDataType.List)
			{
				CreateListFile(sheetname, outputDir, valueRange);
			}
		}
        AssetDatabase.Refresh();

		Debug.Log ("Download completed.");
	}

	private static void CreateJsonFile(string fileName, string outputDirectory, ValueRange valueRange)
	{
		//Get properties's name, data type and sheet data
		IDictionary<int, string> propertyNames = new Dictionary<int, string>();	//Dictionary of (column index, property name of that column)
		IDictionary<int, string> dataTypes = new Dictionary<int, string>();		//Dictionary of (column index, data type of that column)
		IDictionary<int, Dictionary<int, string>> values = new Dictionary<int, Dictionary<int, string>>();	//Dictionary of (row index, dictionary of (column index, value in cell))
		int rowIndex = 0;
		foreach(IList<object> row in valueRange.Values)
		{
			int columnIndex = 0;
			foreach (string cellValue in row) {
				string value = cellValue;
				if(rowIndex == 0)
				{//This row is properties's name row
					propertyNames.Add(columnIndex, value);
				}
				else if(rowIndex == 1)
				{//This row is properties's data type row
					dataTypes.Add(columnIndex, value);
				}
				else
				{//Data rows
					//Because first row is name row and second row is data type row, so we will minus 2 from rowIndex to make data index start from 0
					if(!values.ContainsKey(rowIndex - 2))
					{
						values.Add(rowIndex - 2, new Dictionary<int, string>());
					}
					values[rowIndex - 2].Add(columnIndex, value);
				}

				columnIndex++;
			}

			rowIndex++;
		}

		//Create list of Dictionaries (property name, value). Each dictionary represent for a object in a row of sheet.
		List<Dictionary<string, object>> datas = new List<Dictionary<string, object>>();
		foreach(int rowId in values.Keys)	
		{
			bool thisRowHasError = false;
			Dictionary<string, object> data = new Dictionary<string, object>();
			foreach(int columnId in propertyNames.Keys)	
			{//Read through all columns in sheet, with each column, create a pair of property(string) and value(type depend on dataType[columnId])
				if(thisRowHasError) break;
				if((!dataTypes.ContainsKey(columnId))||(!allowedDataTypes.Contains(dataTypes[columnId])))
					continue;	//There is not any data type or this data type is strange. May be this column is used for comments. Skip this column.
				if(!values[rowId].ContainsKey(columnId))
				{
					values[rowId].Add(columnId, "");
				}

				string strVal = values[rowId][columnId];

				switch(dataTypes[columnId])
				{
					case "string":
					{
						data.Add(propertyNames[columnId], strVal);
						break;
					}
					case "int":
					{
						int val = 0;
						if(!string.IsNullOrEmpty(strVal))
						{
							try
							{
								val = int.Parse(strVal);
							}
							catch(System.Exception e)
							{
								Debug.LogError(string.Format("There is exception when parse value of property {0} of {1} class.\nDetail: {2}",  propertyNames[columnId], fileName, e.ToString()));
								thisRowHasError = true;
								continue;
							}
						}
						data.Add(propertyNames[columnId], val);
						break;
					}
					case "bool":
					{
						bool val = false;
						if(!string.IsNullOrEmpty(strVal))
						{
							try
							{
								val = bool.Parse(strVal);
							}
							catch(System.Exception e)
							{
								Debug.LogError(string.Format("There is exception when parse value of property {0} of {1} class.\nDetail: {2}",  propertyNames[columnId], fileName, e.ToString()));
								continue;
							}
						}
						data.Add(propertyNames[columnId], val);
						break;
					}
					case "float":
					{
						float val = 0f;
						if(!string.IsNullOrEmpty(strVal))
						{
							try
							{
								val = float.Parse(strVal, CultureInfo.InvariantCulture.NumberFormat);
							}
							catch(System.Exception e)
							{
								Debug.LogError(string.Format("There is exception when parse value of property {0} of {1} class.\nDetail: {2}, Value: {3}",  propertyNames[columnId], fileName, e.ToString(), strVal));
								continue;
							}
						}
						data.Add(propertyNames[columnId], val);
						break;
					}
					case "string[]":
					{
						string[] valArr = strVal.Split(new char[]{','});
						data.Add(propertyNames[columnId], valArr);
						break;
					}
					case "int[]":
					{
						string[] strValArr = strVal.Split(new char[]{','});
						int[] valArr = new int[strValArr.Length];
						if (string.IsNullOrEmpty (strVal.Trim ())) {
							valArr = new int[0];
						}
						bool error = false;
						for(int i = 0; i < valArr.Length; i++)
						{
							int val = 0;
							if(!string.IsNullOrEmpty(strValArr[i]))
							{
								try
								{
									val = int.Parse(strValArr[i]);
								}
								catch(System.Exception e)
								{
									Debug.LogError(string.Format("There is exception when parse value of property {0} of {1} class.\nDetail: {2}",  propertyNames[columnId], fileName, e.ToString()));
									error = true;
									break;
								}
							}
							valArr[i] = val;
						}
						if(error)
							continue;
						data.Add(propertyNames[columnId], valArr);
						break;
					}
					case "bool[]":
					{
						string[] strValArr = strVal.Split(new char[]{','});
						bool[] valArr = new bool[strValArr.Length];
						if (string.IsNullOrEmpty (strVal.Trim ())) {
							valArr = new bool[0];
						}
						bool error = false;
						for(int i = 0; i < valArr.Length; i++)
						{
							bool val = false;
							if(!string.IsNullOrEmpty(strValArr[i]))
							{
								try
								{
									val = bool.Parse(strValArr[i]);
								}
								catch(System.Exception e)
								{
									Debug.LogError(string.Format("There is exception when parse value of property {0} of {1} class.\nDetail: {2}",  propertyNames[columnId], fileName, e.ToString()));
									error = true;
									break;
								}
							}
							valArr[i] = val;
						}
						if(error)
							continue;
						data.Add(propertyNames[columnId], valArr);
						break;
					}
					case "float[]":
					{
						string[] strValArr = strVal.Split(new char[]{','});
						float[] valArr = new float[strValArr.Length];
						if (string.IsNullOrEmpty (strVal.Trim ())) {
							valArr = new float[0];
						}
						bool error = false;
						for(int i = 0; i < valArr.Length; i++)
						{
							float val = 0f;
							if(!string.IsNullOrEmpty(strValArr[i]))
							{
								try
								{
									val = float.Parse(strValArr[i]);
								}
								catch(System.Exception e)
								{
									Debug.LogError(string.Format("There is exception when parse value of property {0} of {1} class.\nDetail: {2}",  propertyNames[columnId], fileName, e.ToString()));
									error = true;
									break;
								}
							}
							valArr[i] = val;
						}
						if(error)
							continue;
						data.Add(propertyNames[columnId], valArr);
						break;
					}
					default: break;	//This data type is strange, may be this column is used for comments, not for store data, so do nothing and read next column.
				}
			}

			if(!thisRowHasError)
			{
				datas.Add(data);
			}
			else
			{
				Debug.LogError("There's error!");
			}
		}

		//Create json text
		string jsonText = JsonConvert.SerializeObject(datas);

		//Create directory to store the json file
		if(!outputDirectory.EndsWith("/"))
			outputDirectory += "/";
		Directory.CreateDirectory(outputDirectory);
		StreamWriter strmWriter = new StreamWriter(outputDirectory + fileName + ".json", false, System.Text.Encoding.UTF8);
		strmWriter.Write(jsonText);
		strmWriter.Close();

		Debug.Log ("Created: " + fileName + ".json");
	}

	private static void CreateCSVFile(string fileName, string outputDirectory, ValueRange valueRange)
	{
		StringBuilder stringBuilder = new StringBuilder();
		string propertiesLine = "Id,";

		//Get properties's name, data type and sheet data
		IDictionary<int, string> propertyNames = new Dictionary<int, string>(); //Dictionary of (column index, property name of that column)
		IDictionary<int, Dictionary<int, string>> values = new Dictionary<int, Dictionary<int, string>>();  //Dictionary of (row index, dictionary of (column index, value in cell))
		int rowIndex = 0;
		foreach (IList<object> row in valueRange.Values)
		{
			int columnIndex = 0;
			foreach (string cellValue in row)
			{
				string value = cellValue;
				if (rowIndex == 0)
				{//This row is properties's name row
					propertyNames.Add(columnIndex, value);
					value = ClearStringFromQuotes(value);
					value = ClearStringFromCommas(value);
					propertiesLine += value + ",";
				}
				else
				{//Data rows
				 //Because first row is name row and second row is data type row, so we will minus 2 from rowIndex to make data index start from 0
					if (!values.ContainsKey(rowIndex - 2))
					{
						values.Add(rowIndex - 2, new Dictionary<int, string>());
					}
					values[rowIndex - 2].Add(columnIndex, value);
				}

				columnIndex++;
			}

			rowIndex++;
		}
		stringBuilder.AppendLine(propertiesLine);

		//Create list of Dictionaries (property name, value). Each dictionary represent for a object in a row of sheet.
		List<Dictionary<string, object>> datas = new List<Dictionary<string, object>>();
		int id = 0;
		foreach (int rowId in values.Keys)
		{
			string rowLine = id + ",";
			bool thisRowHasError = false;
			Dictionary<string, object> data = new Dictionary<string, object>();
			foreach (int columnId in propertyNames.Keys)
			{//Read through all columns in sheet, with each column, create a pair of property(string) and value(type depend on dataType[columnId])
				if (thisRowHasError) break;
				if (!values[rowId].ContainsKey(columnId))
				{
					values[rowId].Add(columnId, "");
				}

				string strVal = values[rowId][columnId];

				data.Add(propertyNames[columnId], strVal);
				strVal = ClearStringFromQuotes(strVal);
				if (StringContainsCommas(strVal))
                {
					rowLine += $"\"{strVal}\",";
				}
				else
                {
					rowLine += strVal + ",";
				}
			}

			if (!thisRowHasError)
			{
				datas.Add(data);
				stringBuilder.AppendLine(rowLine);
			}
			else
			{
				Debug.LogError("There's error!");
			}
		}

		//Create directory to store the json file
		if (!outputDirectory.EndsWith("/"))
			outputDirectory += "/";
		Directory.CreateDirectory(outputDirectory);
		StreamWriter strmWriter = new StreamWriter(outputDirectory + fileName + ".csv", false, System.Text.Encoding.UTF8);
		strmWriter.Write(stringBuilder);
		strmWriter.Close();

		Debug.Log("Created: " + fileName + ".csv");
	}

	private static void CreateListFile(string fileName, string outputDirectory, ValueRange valueRange)
	{
		StringBuilder stringBuilder = new StringBuilder();

		IDictionary<int, Dictionary<int, string>> values = new Dictionary<int, Dictionary<int, string>>();  //Dictionary of (row index, dictionary of (column index, value in cell))
		int rowIndex = 0;
		foreach (IList<object> row in valueRange.Values)
		{
			int columnIndex = 0;
			foreach (string cellValue in row)
			{
				string value = cellValue;
				if (!values.ContainsKey(rowIndex))
				{
					values.Add(rowIndex, new Dictionary<int, string>());
				}
				values[rowIndex].Add(columnIndex, value);
				columnIndex++;
			}
			rowIndex++;
		}

		foreach (int rowId in values.Keys)
		{
			string rowLine = string.Empty;
			int columnId = 0;
			if (!values[rowId].ContainsKey(columnId))
			{
				values[rowId].Add(columnId, "");
			}

			string strVal = values[rowId][columnId];

			strVal = ClearStringFromQuotes(strVal);
			if (StringContainsCommas(strVal))
			{
				rowLine += $"\"{strVal}\"";
			}
			else
			{
				rowLine += strVal;
			}
			stringBuilder.AppendLine(rowLine);
		}

		//Create directory to store the json file
		if (!outputDirectory.EndsWith("/"))
			outputDirectory += "/";
		Directory.CreateDirectory(outputDirectory);
		StreamWriter strmWriter = new StreamWriter(outputDirectory + fileName + ".txt", false, System.Text.Encoding.UTF8);
		strmWriter.Write(stringBuilder);
		strmWriter.Close();

		Debug.Log("Created: " + fileName + ".txt");
	}

	private static bool StringContainsCommas(string value)
	{
		return value.Contains(",");
	}

	private static string ClearStringFromCommas(string value)
    {
		return value.Replace(",", "");
    }

	private static string ClearStringFromQuotes(string value)
	{
		return value.Replace("\"", "");
	}

	private static UserCredential GetCredential()
	{
		MonoScript ms = MonoScript.FromScriptableObject(ScriptableObject.CreateInstance(typeof(GoogleSheetsToFiles)));
		string scriptFilePath = AssetDatabase.GetAssetPath(ms);
		FileInfo fi = new FileInfo( scriptFilePath);
		string scriptFolder = fi.Directory.ToString();
		scriptFolder.Replace( '\\', '/');
		Debug.Log ("Save Credential to: " + scriptFolder);

		UserCredential credential = null;
		ClientSecrets clientSecrets = new ClientSecrets ();
		clientSecrets.ClientId = CLIENT_ID;
		clientSecrets.ClientSecret = CLIENT_SECRET;
		try
		{
		credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
			clientSecrets,
			Scopes,
			"user",
			CancellationToken.None,
			new FileDataStore(scriptFolder, true)).Result;
		}
		catch (Exception e) {
			Debug.LogError (e.ToString ());
		}

		return credential;
	}

	private static bool MyRemoteCertificateValidationCallback(System.Object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) {
		bool isOk = true;
		// If there are errors in the certificate chain, look at each error to determine the cause.
		if (sslPolicyErrors != SslPolicyErrors.None) {
			for(int i=0; i<chain.ChainStatus.Length; i++) {
				if(chain.ChainStatus[i].Status != X509ChainStatusFlags.RevocationStatusUnknown) {
					chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;
					chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
					chain.ChainPolicy.UrlRetrievalTimeout = new TimeSpan(0, 1, 0);
					chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllFlags;
					bool chainIsValid = chain.Build((X509Certificate2)certificate);
					if(!chainIsValid) {
						Debug.LogError ("certificate chain is not valid");
						isOk = false;
					}
				}
			}
		}
		return isOk;
	}
}
