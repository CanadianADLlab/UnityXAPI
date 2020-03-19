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
    public string agent = "OTHER";//represents the recruiting center

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
               "1ba5bde68aa8aa71694b3f7ef3a5f5f06fa51d85",
               "ccfe50fbd8e158e2b938f8d60fd901d337c9f586"
               );
            }

        }));

    }

    /// <summary>
    /// Creates and ques and lrs statement to wait to upload
    /// </summary>
    /// <param name="activity">What the user was going</param>
    /// <param name="agent">The users name</param>
    /// <param name="verbName">How they did something (eg completed)</param>
    /// <param name="_type">The type or category of activity</param>
    /// <param name="time">Time since scene began if left null </param>
    /// <param name="newDateTime">If you want to override the datetime rather than it being datetime.now</param>
    public void CreateStatement(string activity = "", string agent = "", string verbName = "", string _type = "", float time = 0, string newDateTime = "")
    {
        StartCoroutine(QueueLRSStatement(activity, agent, verbName, _type, time, newDateTime));
    }

    public IEnumerator QueueLRSStatement(string activity = "", string agent = "", string verbName = "",string _type = "",float time = 0, string newDateTime = "")
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

        actor.mbox = "mailto:" + agent;
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
        activityOBJ.definition.type = new Uri("http://not/real/" + _type);

        var result = new Result();

        var statement = new Statement();
        statement.actor = actor;
        statement.verb = verb;
        statement.target = activityOBJ;

        if (newDateTime.Equals(String.Empty)) // if the datetime doesn't exist, it is a new statement
        {
            statement.timestamp = DateTime.Now;
            SaveData.Instance.AddStatement(activity, agent, verbName, _type, DateTime.Now.ToString("O"), time); // if it is a new statment we will keep track of it to save in the future
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
    private void OnApplicationPause(bool pause)
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
