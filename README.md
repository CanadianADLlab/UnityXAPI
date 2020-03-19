Setting up XAPI for unity

1) Take the package from this repo and drop it into any project <br>
2) Drop the AnalyticsManager prefab into anyscene (It is don't destroy on load so it only really needs to be in the first scene)
3) Now to create a statement all you need to do is reference the singleton.
4) A statment looks as follow : AnalyticsManager.analyticsManager.CreateStatement("Activity","Name","Email","Verb");
5) There are a few other optional paremeters such as activity time, if null it is just how long since scene has loaded.There is date,which by default is DateTime.Now , and finally type which is the category of activity (eg "Test","Lesson").
6) If an upload fails the manager will write it to a file stored locally that will be loaded in next attempt.
7) For this example project, statements are uploaded on ApplicationExit or ApplicationPause by calling the coroutine upload statements. If you want to change this just delete those functions and call it via StartCoroutine(AnalyticsManager.analyticsManager.UploadStatements()); 