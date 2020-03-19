using UnityEngine;
using System.Collections;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Manages writing data 
/// </summary>
public class SaveData : MonoBehaviour
{ 
    public static SaveData Instance;
    public const string FILENAME = "/XAPIData.dat";

    //public bool LoadSuccess = false;

    private SaveInformation saveInfo;


    void Awake()
    {
        if (Instance == null)
        {
            DontDestroyOnLoad(gameObject);
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
   
    }


    public void AddStatement(string activityName, string agent,string email,string verbName ,string _type,string dateTime, float time = 0)
    {
      
        XAPIStatementData data = new XAPIStatementData();
        if (saveInfo == null)
        {
            saveInfo = new SaveInformation
            {
                XAPIList = new List<XAPIStatementData>()
            };
        }
    
        data.activityName = activityName;
        data.agent = agent;
        data.email = email;
        data.verbName = verbName;
        data.type = _type;
        data.statementDateTime = dateTime;
        data.timeSpent = time;

        saveInfo.XAPIList.Add(data);
    }

    public void Save()
    {
        if (saveInfo != null && saveInfo.XAPIList.Count > 0) 
        {
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file;
            file = File.Create(Application.persistentDataPath + FILENAME);
            bf.Serialize(file, saveInfo);
            file.Close();
        }
    }

    public void RemoveFile()
    {
        if(CheckIfFileExist())
        {
            File.Delete(Application.persistentDataPath + FILENAME);
        }
    }


    public IEnumerator Load()
    {
        if (CheckIfFileExist())
        {
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Open(Application.persistentDataPath + FILENAME, FileMode.Open);
            if (file.Length > 0)
            {
                SaveInformation data = (SaveInformation)bf.Deserialize(file);
                file.Close();
                foreach (XAPIStatementData newData in data.XAPIList)
                {
                    AddStatement(newData.activityName, newData.agent,newData.email ,newData.verbName, newData.type, newData.statementDateTime, newData.timeSpent); // if existy we add it to the list
                }
                yield return null;
                StartCoroutine(LoadOldStatements());
            }
            else
            {
                file.Close();
                RemoveFile();
            }
        }
        else
        {
            //LoadSuccess = true;
        }
    }

    private IEnumerator LoadOldStatements()
    {
        if (saveInfo != null)
        {
            foreach (XAPIStatementData statement in saveInfo.XAPIList)
            {
                StartCoroutine(AnalyticsManager.analyticsManager.QueueLRSStatement(statement.activityName, statement.agent,statement.email, statement.verbName,statement.type ,statement.timeSpent, statement.statementDateTime));
            }
        }
        yield return null; 
    }
  
    public bool CheckIfFileExist()
    {
        return File.Exists(Application.persistentDataPath + FILENAME);
    }
}
[Serializable]
class SaveInformation
{
  public List<XAPIStatementData> XAPIList; 
}

[Serializable]
class XAPIStatementData
{
    public string activityName;
    public string agent;
    public string email;
    public string verbName;
    public string type;
    public float timeSpent;
    public string statementDateTime;
}
