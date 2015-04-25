using System;
using System.Collections;
using System.Collections.Generic;
using HTTP;
using UnityEngine;

namespace API
{
	public class CreditController: APIConnection
	{
		private const string CREDIT = "credit";
		
		protected CreditController()
		{
		}
		
		private static readonly CreditController _Instance = new CreditController();
		
		/// <summary>
		///     Gets the instance.
		/// </summary>
		/// <value>The instance.</value>
		public static CreditController Instance
		{
			get { return _Instance; }
		}

		public Request AddCredit(CreditModel cm, Action<Response> success = null, Action<API_Error> error = null)
		{
			return PostJsonRequest (BASE_URL + CREDIT, cm.ToHash(), success, error);
		}
	}

	public class CreditModel
	{
		public CreditActions Action { get; set; }
		public int id { get; set; }

		public Hashtable ToHash()
		{
			var hash = new Hashtable {
			    {"Actions", (int)Action},
				{"ID", id}
			};

			return hash;
		}
		
		public static CreditModel Create(Hashtable dict)
		{
			return null;
		}
	}

	public enum CreditActions { 
		ENTERMUSEUM, 
		BUILDEDMUSEUM 
	}
}

