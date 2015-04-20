using System;
using HTTP;
using UnityEngine;

namespace API
{
    /// <summary>
    ///     User controller. Does all request concerning the User, such as OAuth, and user management tasks.
    /// </summary>
    public class UserController : APIConnection
    {
        protected UserController()
        {
        }

        private static readonly UserController _Instance = new UserController();

        public static UserController Instance
        {
            get { return _Instance; }
        }

        public Request CreateUser(string username, string email, string password, Action<User> succes = null,
            Action<API_Error> error = null)
        {
            return Post(BASE_URL + "account/register",
                new[] {"UserName", "Email", "Password", "ConfirmPassword"},
                new[] {username, email, password, password},
                (response => { Login(username, password, succes, error); }), error, false);
        }

        public bool UpdateCredit(User user, int credit)
        {
            //TODO: needs to be implemented in API
            return false;
        }

        public Request Login(string username, string password, Action<User> succes = null,
            Action<API_Error> error = null)
        {
            return Post("http://api.awesomepeople.tv/Token",
                new[] {"grant_type", "username", "password"},
                new[] {"password", username, password},
                (response =>
                {
                    var token = Token.CreateFromDictionary(response.Object);
                    Debug.Log(token.AccessToken());
                    var user = new User((string) response.Object["userName"], token);
                    if (succes != null)
                    {
                        succes(user);
                    }
                }), error, false);
        }
    }
}
