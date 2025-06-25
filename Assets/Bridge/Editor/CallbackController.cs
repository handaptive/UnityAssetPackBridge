using System.Collections;
using UnityEngine;
using Unity.EditorCoroutines.Editor;

namespace AssetPack.Bridge.Editor
{
  public class CallbackController
  {
    public event System.Action Finished = null;
    public event System.Action<string> Errored = null;

    private EditorCoroutine _routine = null;

    public void Stop()
    {
      if (_routine != null)
      {
        EditorCoroutineUtility.StopCoroutine(_routine);
      }

      _routine = null;
    }

    public void Start()
    {
      Stop();
      _routine = EditorCoroutineUtility.StartCoroutine(RunCallbackFlow(), this);
    }

    private IEnumerator RunCallbackFlow()
    {
      string id = null;
      bool error = false;

      void handleError(string errorMessage)
      {
        error = true;
        Errored?.Invoke(errorMessage);
        _routine = null;
      }

      // Create a callback
      yield return BridgeAPI.CreateCallback(new RequestArgs<CallbackCreateOutput>
      {
        onSuccess = (output) =>
        {
          id = output.id;
        },
        onError = handleError,
      });
      if (error)
      {
        yield break;
      }

      Utility.Log($"Opening login URL with callback ID: {id}");
      Application.OpenURL($"{Utility.GetWebsiteUrl()}/login?callback={id}");

      // Poll the server for the callback status
      CallbackPollOutput successOutput = null;
      while (successOutput == null)
      {
        yield return Utility.WaitForSeconds(3f);

        yield return BridgeAPI.PollCallback(
          new RequestArgs<CallbackPollOutput>
          {
            onSuccess = (output) =>
            {
              Utility.Log($"Callback status: {output.status}");
              if (output.status == "success")
              {
                successOutput = output;
              }
              else if (output.status == "error")
              {
                Utility.LogError($"Callback error: {output.error}");
                handleError(output.error);
              }
            },
            onError = handleError,
          },
          new CallbackPollInput { id = id }
        );
        if (error)
        {
          yield break;
        }
      }

      // Set tokens
      Utility.SetIdToken(successOutput.idToken);
      Utility.SetRefreshToken(successOutput.refreshToken);

      Utility.Log("Callback completed successfully.");
      Finished?.Invoke();
    }
  }
}
