using System;
using System.Text;
using System.Collections;
using UnityEngine;
using System.Security.Cryptography;
using System.IO;
using BestHTTP;

[Serializable]
public class VWSResponse
{
	public string result_code;
	public string transaction_id;
	public string target_id;
	public VWSTargetRecord target_record;
	public string[] similar_targets;
	public string[] results;
	public string status;


	public VWSResponse(string result_code)
	{
		this.result_code = result_code;
	}
}

[Serializable]
public class VWSTargetSummary
{
	public string result_code;
	public string transaction_id;
	public string database_name;
	public string target_name;
	public string upload_date;
	public bool active_flag;
	public string status;
	public int tracking_rating;
	public string reco_rating;
	public int total_recos;
	public int current_month_recos;
	public int previous_month_recos;

	public VWSTargetSummary(string result_code)
	{
		this.result_code = result_code;
	}
}

[Serializable]
public class VWSDatabaseSummary
{
	public string result_code;
	public string transaction_id;
	public string name;
	public int active_images;
	public int inactive_images;
	public int failed_images;

	public VWSDatabaseSummary(string result_code)
	{
		this.result_code = result_code;
	}
}


[Serializable]
public class VWSTargetRecord
{
	public string target_id;
	public string name;
	public float width;
	public int tracking_rating;
	public bool active_flag;
	public string reco_rating;
}

[Serializable]
public class VWSTarget
{
	public string name;
	public float width;
	public string image;
	public bool active_flag;
	public string application_metadata;

	public VWSTarget (string name, float width, string image, bool active_flag, string application_metadata)
	{
		this.name = name;
		this.width = width;
		this.image = image;
		this.active_flag = active_flag;
		this.application_metadata = application_metadata;
	}
}

public class VWS : MonoBehaviour 
{
	public static VWS Instance;

	public string accessKey;
	public string secretKey;

	private const string vwsUrl = "https://vws.vuforia.com";

	void Awake() 
	{
		if (Instance == null)
		{
			Instance = this;
		} 
		else
		{
			Destroy(gameObject);
		}
	}

	public void AddTarget(string targetName, float width, Texture2D image, bool active_flag, string metadata, Action<VWSResponse> response)
	{
		string imageString = Convert.ToBase64String(image.EncodeToJPG());
		string meta = Convert.ToBase64String(Encoding.UTF8.GetBytes(metadata));

		VWSTarget newTarget = new VWSTarget(targetName, width, imageString, active_flag, meta);

		string content = JsonUtility.ToJson(newTarget); 
		string[] query = new string[5];

		query[0] = "POST"; // method
		query[1] = CalculateMD5Hash(content).ToLower(); // content
		query[2] = "application/json"; // content type
		query[3] = DateTime.UtcNow.GetDateTimeFormats()[15]; // date
		query[4] = "/targets"; // url

		string stringToSign = string.Join("\n", query);

		string signature = "VWS " + accessKey + ":" + BuildSignature(secretKey, stringToSign);
		StartCoroutine(AddTargetCoroutine(signature, content, query, httpResponse => 
			{
				response(httpResponse);
			}));
	}

	IEnumerator AddTargetCoroutine (string signature, string content, string[] query, Action<VWSResponse> response)
	{
		HTTPRequest request = new HTTPRequest(new Uri(vwsUrl + query[4]));

		request.MethodType = HTTPMethods.Post;
		request.RawData = Encoding.UTF8.GetBytes(content);
		request.AddHeader("Authorization", signature);
		request.AddHeader("Content-Type", query[2]);
		request.AddHeader("Date", query[3]);
		request.Send();

		yield return StartCoroutine(request);

		switch (request.State) 
		{
			case HTTPRequestStates.Finished:
				response(JsonUtility.FromJson<VWSResponse>(request.Response.DataAsText));
				break;

			case HTTPRequestStates.Error:
				Debug.LogWarning("Request Finished with Error. " + (request.Exception != null ? (request.Exception.Message + "\n" + request.Exception.StackTrace) : "No Exception"));
				response(new VWSResponse("Request Finished with Error"));
				break;

			case HTTPRequestStates.Aborted:
				Debug.LogWarning("Request Aborted");
				response(new VWSResponse("Request Aborted"));
				break;

			case HTTPRequestStates.ConnectionTimedOut:
				Debug.LogError("Connection Timed Out");
				response(new VWSResponse("Connection Timed Out"));
				break;

			case HTTPRequestStates.TimedOut:
				Debug.LogError("Processing the request Timed Out");
				response(new VWSResponse("Processing the request Timed Out"));
				break;
		}
	}

	public void UpdateTarget(string targetID, string targetName, float width, Texture2D image, bool active_flag, string metadata, Action<VWSResponse> response)
	{
		string imageString = Convert.ToBase64String(image.EncodeToJPG());
		string meta = Convert.ToBase64String(Encoding.UTF8.GetBytes(metadata));

		VWSTarget newTarget = new VWSTarget(targetName, width, imageString, active_flag, meta);

		string content = JsonUtility.ToJson(newTarget); 
		string[] query = new string[5];

		query[0] = "PUT"; // method
		query[1] = CalculateMD5Hash(content).ToLower(); // content
		query[2] = "application/json"; // content type
		query[3] = DateTime.UtcNow.GetDateTimeFormats()[15]; // date
		query[4] = "/targets/" + targetID; // url

		string stringToSign = string.Join("\n", query);
		string signature = "VWS " + accessKey + ":" + BuildSignature(secretKey, stringToSign);
		StartCoroutine(UpdateTargetCoroutine(signature, content, query, httpResponse => 
			{
				response(httpResponse);
			}));
	}

	public void UpdateTargetName(string targetID, string targetNewName, Action<VWSResponse> response)
	{
		string content = "{\"name\":\"" + targetNewName + "\"}"; 
		string[] query = new string[5];

		query[0] = "PUT"; // method
		query[1] = CalculateMD5Hash(content).ToLower(); // content
		query[2] = "application/json"; // content type
		query[3] = DateTime.UtcNow.GetDateTimeFormats()[15]; // date
		query[4] = "/targets/" + targetID; // url

		string stringToSign = string.Join("\n", query);

		string signature = "VWS " + accessKey + ":" + BuildSignature(secretKey, stringToSign);
		StartCoroutine(UpdateTargetCoroutine(signature, content, query, httpResponse => 
			{
				response(httpResponse);
			}));
	}

	public void UpdateTargetWidth(string targetID, float targetNewWidth, Action<VWSResponse> response)
	{
		string content = "{\"width\":" + targetNewWidth + "}"; 
		string[] query = new string[5];

		query[0] = "PUT"; // method
		query[1] = CalculateMD5Hash(content).ToLower(); // content
		query[2] = "application/json"; // content type
		query[3] = DateTime.UtcNow.GetDateTimeFormats()[15]; // date
		query[4] = "/targets/" + targetID; // url

		string stringToSign = string.Join("\n", query);

		string signature = "VWS " + accessKey + ":" + BuildSignature(secretKey, stringToSign);
		StartCoroutine(UpdateTargetCoroutine(signature, content, query, httpResponse => 
			{
				response(httpResponse);
			}));
	}

	public void UpdateTargetImage(string targetID, Texture2D image, Action<VWSResponse> response)
	{
		string imageString = Convert.ToBase64String(image.EncodeToJPG());
		string content = "{\"image\":\"" + imageString + "\"}"; 
		string[] query = new string[5];

		query[0] = "PUT"; // method
		query[1] = CalculateMD5Hash(content).ToLower(); // content
		query[2] = "application/json"; // content type
		query[3] = DateTime.UtcNow.GetDateTimeFormats()[15]; // date
		query[4] = "/targets/" + targetID; // url

		string stringToSign = string.Join("\n", query);

		string signature = "VWS " + accessKey + ":" + BuildSignature(secretKey, stringToSign);
		StartCoroutine(UpdateTargetCoroutine(signature, content, query, httpResponse => 
			{
				response(httpResponse);
			}));
	}

	public void UpdateTargetFlag(string targetID, bool active_flag, Action<VWSResponse> response)
	{
		string content = "{\"active_flag\":" + active_flag.ToString().ToLower() + "}"; 
		string[] query = new string[5];

		query[0] = "PUT"; // method
		query[1] = CalculateMD5Hash(content).ToLower(); // content
		query[2] = "application/json"; // content type
		query[3] = DateTime.UtcNow.GetDateTimeFormats()[15]; // date
		query[4] = "/targets/" + targetID; // url

		string stringToSign = string.Join("\n", query);

		string signature = "VWS " + accessKey + ":" + BuildSignature(secretKey, stringToSign);
		StartCoroutine(UpdateTargetCoroutine(signature, content, query, httpResponse => 
			{
				response(httpResponse);
			}));
	}

	public void UpdateTargetMetadata(string targetID, string metadata, Action<VWSResponse> response)
	{
		string meta = Convert.ToBase64String(Encoding.UTF8.GetBytes(metadata));
		string content = "{\"application_metadata\":\"" + meta + "\"}"; 
		string[] query = new string[5];

		query[0] = "PUT"; // method
		query[1] = CalculateMD5Hash(content).ToLower(); // content
		query[2] = "application/json"; // content type
		query[3] = DateTime.UtcNow.GetDateTimeFormats()[15]; // date
		query[4] = "/targets/" + targetID; // url

		string stringToSign = string.Join("\n", query);

		string signature = "VWS " + accessKey + ":" + BuildSignature(secretKey, stringToSign);
		StartCoroutine(UpdateTargetCoroutine(signature, content, query, httpResponse => 
			{
				response(httpResponse);
			}));
	}

	private IEnumerator UpdateTargetCoroutine (string signature, string content, string[] query, Action<VWSResponse> response)
	{
		HTTPRequest request = new HTTPRequest(new Uri(vwsUrl + query[4]));

		request.MethodType = HTTPMethods.Put;
		request.RawData = Encoding.UTF8.GetBytes(content);
		request.AddHeader("Authorization", signature);
		request.AddHeader("Content-Type", query[2]);
		request.AddHeader("Date", query[3]);
		request.Send();

		yield return StartCoroutine(request);

		switch (request.State) 
		{
		case HTTPRequestStates.Finished:
			response(JsonUtility.FromJson<VWSResponse>(request.Response.DataAsText));
			break;

		case HTTPRequestStates.Error:
			Debug.LogWarning("Request Finished with Error. " + (request.Exception != null ? (request.Exception.Message + "\n" + request.Exception.StackTrace) : "No Exception"));
			response(new VWSResponse("Request Finished with Error"));
			break;

		case HTTPRequestStates.Aborted:
			Debug.LogWarning("Request Aborted");
			response(new VWSResponse("Request Aborted"));
			break;

		case HTTPRequestStates.ConnectionTimedOut:
			Debug.LogError("Connection Timed Out");
			response(new VWSResponse("Connection Timed Out"));
			break;

		case HTTPRequestStates.TimedOut:
			Debug.LogError("Processing the request Timed Out");
			response(new VWSResponse("Processing the request Timed Out"));
			break;
		}
	}


	public void DeleteTarget(string targetID, Action<VWSResponse> response)
	{
		string content = ""; 
		string[] query = new string[5];

		query[0] = "DELETE"; // method
		query[1] = CalculateMD5Hash(content).ToLower(); // content
		query[2] = "application/json"; // content type
		query[3] = DateTime.UtcNow.GetDateTimeFormats()[15]; // date
		query[4] = "/targets/" + targetID; // url

		string stringToSign = string.Join("\n", query);
		string signature = "VWS " + accessKey + ":" + BuildSignature(secretKey, stringToSign);
		StartCoroutine(DeleteTargetCoroutine(signature, query, httpResponse => 
			{
				response(httpResponse);
			}));
	}

	private IEnumerator DeleteTargetCoroutine (string signature, string[] query, Action<VWSResponse> response)
	{
		HTTPRequest request = new HTTPRequest(new Uri(vwsUrl + query[4]));

		request.MethodType = HTTPMethods.Delete;
		request.RawData = Encoding.UTF8.GetBytes("");
		request.AddHeader("Authorization", signature);
		request.AddHeader("Content-Type", query[2]);
		request.AddHeader("Date", query[3]);
		request.Send();

		yield return StartCoroutine(request);

		switch (request.State) 
		{
		case HTTPRequestStates.Finished:
			response(JsonUtility.FromJson<VWSResponse>(request.Response.DataAsText));
			break;

		case HTTPRequestStates.Error:
			Debug.LogWarning("Request Finished with Error. " + (request.Exception != null ? (request.Exception.Message + "\n" + request.Exception.StackTrace) : "No Exception"));
			response(new VWSResponse("Request Finished with Error"));
			break;

		case HTTPRequestStates.Aborted:
			Debug.LogWarning("Request Aborted");
			response(new VWSResponse("Request Aborted"));
			break;

		case HTTPRequestStates.ConnectionTimedOut:
			Debug.LogError("Connection Timed Out");
			response(new VWSResponse("Connection Timed Out"));
			break;

		case HTTPRequestStates.TimedOut:
			Debug.LogError("Processing the request Timed Out");
			response(new VWSResponse("Processing the request Timed Out"));
			break;
		}
	}

	public void RetrieveTarget(string targetID, Action<VWSResponse> response)
	{
		string content = ""; 
		string[] query = new string[5];

		query[0] = "GET"; // method
		query[1] = CalculateMD5Hash(content).ToLower(); // content
		query[2] = "application/json"; // content type
		query[3] = DateTime.UtcNow.GetDateTimeFormats()[15]; // date
		query[4] = "/targets/" + targetID; // url

		string stringToSign = string.Join("\n", query);
		string signature = "VWS " + accessKey + ":" + BuildSignature(secretKey, stringToSign);
		StartCoroutine(RetrieveTargetCoroutine(signature, query, httpResponse => 
			{
				response(httpResponse);
			}));
	}

	public void RetrieveTargetDuplicates(string targetID, Action<VWSResponse> response)
	{
		string content = ""; 
		string[] query = new string[5];

		query[0] = "GET"; // method
		query[1] = CalculateMD5Hash(content).ToLower(); // content
		query[2] = "application/json"; // content type
		query[3] = DateTime.UtcNow.GetDateTimeFormats()[15]; // date
		query[4] = "/duplicates/" + targetID; // url

		string stringToSign = string.Join("\n", query);
		string signature = "VWS " + accessKey + ":" + BuildSignature(secretKey, stringToSign);
		StartCoroutine(RetrieveTargetCoroutine(signature, query, httpResponse => 
			{
				response(httpResponse);
			}));
	}

	public void RetrieveTargetList(Action<VWSResponse> response)
	{
		string content = ""; 
		string[] query = new string[5];

		query[0] = "GET"; // method
		query[1] = CalculateMD5Hash(content).ToLower(); // content
		query[2] = "application/json"; // content type
		query[3] = DateTime.UtcNow.GetDateTimeFormats()[15]; // date
		query[4] = "/targets"; // url

		string stringToSign = string.Join("\n", query);
		string signature = "VWS " + accessKey + ":" + BuildSignature(secretKey, stringToSign);
		StartCoroutine(RetrieveTargetCoroutine(signature, query, httpResponse => 
			{
				response(httpResponse);
			}));
	}

	private IEnumerator RetrieveTargetCoroutine (string signature, string[] query, Action<VWSResponse> response)
	{
		HTTPRequest request = new HTTPRequest(new Uri(vwsUrl + query[4]));

		request.MethodType = HTTPMethods.Get;
		request.RawData = Encoding.UTF8.GetBytes("");
		request.AddHeader("Authorization", signature);
		request.AddHeader("Content-Type", query[2]);
		request.AddHeader("Date", query[3]);
		request.Send();

		yield return StartCoroutine(request);

		switch (request.State) 
		{
		case HTTPRequestStates.Finished:
			response(JsonUtility.FromJson<VWSResponse>(request.Response.DataAsText));
			break;

		case HTTPRequestStates.Error:
			Debug.LogWarning("Request Finished with Error. " + (request.Exception != null ? (request.Exception.Message + "\n" + request.Exception.StackTrace) : "No Exception"));
			response(new VWSResponse("Request Finished with Error"));
			break;

		case HTTPRequestStates.Aborted:
			Debug.LogWarning("Request Aborted");
			response(new VWSResponse("Request Aborted"));
			break;

		case HTTPRequestStates.ConnectionTimedOut:
			Debug.LogError("Connection Timed Out");
			response(new VWSResponse("Connection Timed Out"));
			break;

		case HTTPRequestStates.TimedOut:
			Debug.LogError("Processing the request Timed Out");
			response(new VWSResponse("Processing the request Timed Out"));
			break;
		}
	}

	public void RetrieveTargetSummary(string targetID, Action<VWSTargetSummary> response)
	{
		string content = ""; 
		string[] query = new string[5];

		query[0] = "GET"; // method
		query[1] = CalculateMD5Hash(content).ToLower(); // content
		query[2] = "application/json"; // content type
		query[3] = DateTime.UtcNow.GetDateTimeFormats()[15]; // date
		query[4] = "/summary/" + targetID; // url

		string stringToSign = string.Join("\n", query);
		string signature = "VWS " + accessKey + ":" + BuildSignature(secretKey, stringToSign);
		StartCoroutine(RetrieveTargetSummaryCoroutine(signature, query, httpResponse => 
			{
				response(httpResponse);
			}));
	}

	private IEnumerator RetrieveTargetSummaryCoroutine (string signature, string[] query, Action<VWSTargetSummary> response)
	{
		HTTPRequest request = new HTTPRequest(new Uri(vwsUrl + query[4]));

		request.MethodType = HTTPMethods.Get;
		request.RawData = Encoding.UTF8.GetBytes("");
		request.AddHeader("Authorization", signature);
		request.AddHeader("Content-Type", query[2]);
		request.AddHeader("Date", query[3]);
		request.Send();

		yield return StartCoroutine(request);

		switch (request.State) 
		{
		case HTTPRequestStates.Finished:
			response(JsonUtility.FromJson<VWSTargetSummary>(request.Response.DataAsText));
			break;

		case HTTPRequestStates.Error:
			Debug.LogWarning("Request Finished with Error. " + (request.Exception != null ? (request.Exception.Message + "\n" + request.Exception.StackTrace) : "No Exception"));
			response(new VWSTargetSummary("Request Finished with Error"));
			break;

		case HTTPRequestStates.Aborted:
			Debug.LogWarning("Request Aborted");
			response(new VWSTargetSummary("Request Aborted"));
			break;

		case HTTPRequestStates.ConnectionTimedOut:
			Debug.LogError("Connection Timed Out");
			response(new VWSTargetSummary("Connection Timed Out"));
			break;

		case HTTPRequestStates.TimedOut:
			Debug.LogError("Processing the request Timed Out");
			response(new VWSTargetSummary("Processing the request Timed Out"));
			break;
		}
	}

	public void RetrieveDatabaseSummary(Action<VWSDatabaseSummary> response)
	{
		string content = ""; 
		string[] query = new string[5];

		query[0] = "GET"; // method
		query[1] = CalculateMD5Hash(content).ToLower(); // content
		query[2] = "application/json"; // content type
		query[3] = DateTime.UtcNow.GetDateTimeFormats()[15]; // date
		query[4] = "/summary"; // url

		string stringToSign = string.Join("\n", query);
		string signature = "VWS " + accessKey + ":" + BuildSignature(secretKey, stringToSign);
		StartCoroutine(RetrieveDatabaseCoroutine(signature, query, httpResponse => 
			{
				response(httpResponse);
			}));
	}

	private IEnumerator RetrieveDatabaseCoroutine (string signature, string[] query, Action<VWSDatabaseSummary> response)
	{
		HTTPRequest request = new HTTPRequest(new Uri(vwsUrl + query[4]));

		request.MethodType = HTTPMethods.Get;
		request.RawData = Encoding.UTF8.GetBytes("");
		request.AddHeader("Authorization", signature);
		request.AddHeader("Content-Type", query[2]);
		request.AddHeader("Date", query[3]);
		request.Send();

		yield return StartCoroutine(request);

		switch (request.State) 
		{
		case HTTPRequestStates.Finished:
			response(JsonUtility.FromJson<VWSDatabaseSummary>(request.Response.DataAsText));
			break;

		case HTTPRequestStates.Error:
			Debug.LogWarning("Request Finished with Error. " + (request.Exception != null ? (request.Exception.Message + "\n" + request.Exception.StackTrace) : "No Exception"));
			response(new VWSDatabaseSummary("Request Finished with Error"));
			break;

		case HTTPRequestStates.Aborted:
			Debug.LogWarning("Request Aborted");
			response(new VWSDatabaseSummary("Request Aborted"));
			break;

		case HTTPRequestStates.ConnectionTimedOut:
			Debug.LogError("Connection Timed Out");
			response(new VWSDatabaseSummary("Connection Timed Out"));
			break;

		case HTTPRequestStates.TimedOut:
			Debug.LogError("Processing the request Timed Out");
			response(new VWSDatabaseSummary("Processing the request Timed Out"));
			break;
		}
	}

	private static string BuildSignature(string keyString, string stringToSign)
	{
		byte[] key = Encoding.UTF8.GetBytes(keyString);
		byte[] data = Encoding.UTF8.GetBytes(stringToSign);

		HMACSHA1 myhmacsha1 = new HMACSHA1(key);
		myhmacsha1.Initialize();
		MemoryStream stream = new MemoryStream(data);

		byte[] hash = myhmacsha1.ComputeHash(stream);

		string signature = System.Convert.ToBase64String(hash);
		return signature;
	}

	private static string CalculateMD5Hash(string input)
	{
		MD5 md5 = System.Security.Cryptography.MD5.Create();
		byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
		byte[] hash = md5.ComputeHash(inputBytes);

		StringBuilder sb = new StringBuilder();

		for (int i = 0; i < hash.Length; i++)
		{
			sb.Append(hash[i].ToString("X2"));
		}

		return sb.ToString();
	}
}

