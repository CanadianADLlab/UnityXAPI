using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TinCan;
using TinCan.LRSResponses;
using TinCan.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.UI;
using UnityEngine.Networking;

/// <summary>
/// handles creating and uploading all statements
/// </summary>
public class AnalyticsManager : MonoBehaviour
{
    public String URL = "https://lrs.ongarde.net/data/xAPI";
    
    // Keys, default ones point to a dummy LRS
    public string StoreKey = "9323f1983e98c9c6f81fa8d2977ec0a1753102b9";
    public string StoreSecretKey = "665919060272ed8307cc3c1fd07eeaa7aab5d079"; 

    bool hasConnection = false;

    string XAPIPHRASE;
    private RemoteLRS lrs;

    private List<Statement> totalStatements;

    /// <summary>
    /// checks for internet connection
    /// </summary>
    /// <param name="action"></param>
    /// <returns></returns>
    public static IEnumerator CheckInternetConnection(Action<bool> syncResult)
    {
        const string echoServer = "http://google.com";

        bool result;
        using (var request = UnityWebRequest.Head(echoServer))
        {
            request.timeout = 2;
            yield return request.SendWebRequest();
            result = !request.isNetworkError && !request.isHttpError && request.responseCode == 200;
        }
        syncResult(result);
    }

    void Start()
    {
        totalStatements = new List<Statement>(); // We will hold all the statements and send them at the end
        StartCoroutine(SaveData.Instance.Load());

        StartCoroutine(CheckInternetConnection((isConnected) =>
        {
            hasConnection = isConnected;
            if (hasConnection)
            {
                print("Internet connection");
                lrs = new RemoteLRS(
                URL,
                StoreKey,
                StoreSecretKey
               );
            }
        }));

    }

    /// <summary>
    /// Creates and ques and lrs statement to wait to upload
    /// </summary>
    /// <param name="activity">What the user was going</param>
    /// <param name="agent">The users name</param>
    /// <param name="email">The users email</param>
    /// <param name="verbName">How they did something (eg completed)</param>
    /// <param name="_type">The type or category of activity</param>
    /// <param name="time">Time since scene began if left null </param>
    /// <param name="newDateTime">If you want to override the datetime rather than it being datetime.now</param>
    public void CreateStatement(string activity = "" ,string agent = "",string email = "", string verbName = "", string _type = "", float time = 0, string newDateTime = "")
    {
        StartCoroutine(QueueLRSStatement(activity, agent, email, verbName, _type, time, newDateTime));
    }

    public IEnumerator QueueLRSStatement(string activity = "", string agent = "", string email = "",string verbName = "",string _type = "",float time = 0, string newDateTime = "")
    {

        var actor = new Agent();
        if (agent == String.Empty)
        {
            agent = "NOAGENT";
        }

        if (activity == String.Empty)
        {
            activity = "NOACTIVITY";
        }

        if(time == 0)
        {
            time = Time.timeSinceLevelLoad;
        }

        if(email == String.Empty)
        {
            actor.mbox = "mailto:" + agent;
        }
        else
        {
            if (!@email.Contains("mailto:"))
            {
                actor.mbox = "mailto:" + email;
            }
            else
            {
                actor.mbox = email;
            }
        }

        actor.name = agent;
        var verb = new Verb();
        verb.id = new Uri("http://adlnet.gov/expapi/verbs/" + verbName);
        verb.display = new LanguageMap();
        verb.display.Add("en-US", verbName);


        XAPIPHRASE = "{\"object\":{\"id\":\"http://" + activity + "\"," +
                          "\"definition\":{\"name\":{\"en-US\":\"" + activity + "\"}}}}";

        StringOfJSON jString = new StringOfJSON(XAPIPHRASE);
        JObject jObj = jString.toJObject();

        var activityOBJ = new Activity(jObj.Value<JObject>("object"));
        if (_type != string.Empty)
        {
            activityOBJ.definition.type = new Uri("http://" + _type);
        }

        var result = new Result();

        var statement = new Statement();
        statement.actor = actor;
        statement.verb = verb;
        statement.target = activityOBJ;

        if (newDateTime.Equals(String.Empty)) // if the datetime doesn't exist, it is a new statement
        {
            statement.timestamp = DateTime.Now;
            SaveData.Instance.AddStatement(activity, agent, actor.mbox, verbName, _type, DateTime.Now.ToString("O"), time); // if it is a new statment we will keep track of it to save in the future
        }
        else
        {
            statement.timestamp = DateTime.Parse(newDateTime);
        }

        if (verbName.Equals("completed"))
        {
            result.completion = true;
            result.success = true;
        }

        result.duration = TimeSpan.FromSeconds(time);
        statement.result = result;

        print("Adding statements " + activity + "-" + actor.mbox + "-" + verbName + "-" + statement.timestamp.ToString() + "-" + time + "-" + _type);

        totalStatements.Add(statement);

        yield return null;
    }


    StatementsResultLRSResponse lrsResponse;
    public IEnumerator UploadStatements()
    {
        if (totalStatements != null && totalStatements.Count > 0) // Don't even try if we don't have any statements
        {
            if (lrs != null && hasConnection)
            {
                print("Connected... Attempting to upload statements to the LRS");

                lrsResponse = lrs.SaveStatements(totalStatements);


                yield return null;
            }
            else // No connection saving the data
            {
                print("No Connection.");
                SaveData.Instance.Save();
            }
        }
    }

    public void ClearStatements()
    {
        totalStatements.Clear(); // We clear when it succeeds
    }

    // If android use application pause, it gets called when app is closed also. For some reason on app quit does not work on android
    private void OnApplicationPause(bool pause)
    {
        StartCoroutine(UploadStatements());
    }

    // Use for regular desktop builds
    private void OnApplicationQuit()
    {
        
        StartCoroutine(UploadStatements());
    }


    #region Singleton
    public static AnalyticsManager analyticsManager = null;

    private void Awake()
    {
        if (analyticsManager == null)
            analyticsManager = this;
        else
            Destroy(this.gameObject);

        DontDestroyOnLoad(this.gameObject);
    }
    private void OnDestroy()
    {
        if (analyticsManager == this)
            analyticsManager = null;
    }
    #endregion
}
