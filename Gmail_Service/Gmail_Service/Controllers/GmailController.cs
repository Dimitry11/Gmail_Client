namespace Gmail_Service.Controllers
{
    using System;
    using System.Net.Mail;
    using Gmail_Service.Models;
    using Google.Apis.Gmail.v1;
    using Google.Apis.Services;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using Google.Apis.Auth.OAuth2;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Http;
    using Google.Apis.Gmail.v1.Data;
    using System.Collections.Generic;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Authentication;

    public class GmailController : Controller
    {
        string accessToken;
        My_Message message;
        static string emailTo;

        async void GetToken()
        {
            accessToken = await HttpContext.GetTokenAsync("access_token");
        }

        GmailService GetService()
        {
            GetToken();
            GoogleCredential credential = GoogleCredential.FromAccessToken(accessToken);
            GmailService service = new GmailService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential
            });

            return service;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetListEmail(string labelId, string nameLabel)
        {
            string userEmail = User.FindFirst(u => u.Type == ClaimTypes.Email).Value;
            GmailService service = GetService();
            List<My_Message> listMessages = new List<My_Message>();
            List<Message> result = new List<Message>();
            var emailListRequest = service.Users.Messages.List(userEmail);
            emailListRequest.LabelIds = labelId;
            emailListRequest.IncludeSpamTrash = false;
            emailListRequest.MaxResults = 1000;
            ListMessagesResponse emailListResponse = await emailListRequest.ExecuteAsync();

            if (emailListResponse != null && emailListResponse.Messages != null)
            {
                foreach (Message email in emailListResponse.Messages)
                {
                    var emailInfoRequest = service.Users.Messages.Get(userEmail, email.Id);
                    var emailInfoResponse = await emailInfoRequest.ExecuteAsync();

                    if (emailInfoResponse != null)
                    {
                        message = new My_Message();
                        message.Id = listMessages.Count + 1;
                        message.EmailId = email.Id;

                        foreach (var mPart in emailInfoResponse.Payload.Headers)
                        {
                            if (mPart.Name == "Date")
                                message.Date_Received = mPart.Value;
                            else if (mPart.Name == "From")
                            {
                                message.From = mPart.Value;
                                emailTo = mPart.Value;
                            }
                            else if (mPart.Name == "Subject")
                                message.Title = mPart.Value;
                        }

                        listMessages.Add(message);
                    }
                }
            }

            ViewBag.Message = nameLabel;
            return View("~/Views/Home/Index.cshtml", listMessages);
        }

        public async Task<IActionResult> GoToSendEmail(string emailId="", string emailFrom = "")
        {
            string UserEmail = User.FindFirst(c => c.Type == ClaimTypes.Email).Value;
            string[] userData = { emailId, emailFrom };
            ViewBag.userDatas = userData;
            string[] words = emailTo.Split(new char[] { '<', '>' });
            return View("SendEmailView", words[1]);
        }

        public ActionResult SendEmail(string subject, string emailTo, string body)
        {
            string UserEmail = User.FindFirst(c => c.Type == ClaimTypes.Email).Value;
            GmailService service = GetService();

            MailMessage msg = new MailMessage();
            msg.From = new MailAddress(UserEmail);
            msg.To.Add(emailTo);
            msg.ReplyToList.Add(UserEmail);
            msg.Subject = UserEmail;
            msg.Body = body;
            msg.IsBodyHtml = true;
            MimeKit.MimeMessage mime = MimeKit.MimeMessage.CreateFromMailMessage(msg);
            Message gmailMsg = new Message
            {
                Raw = Encode(mime.ToString())
            };

            var request = service.Users.Messages.Send(gmailMsg, UserEmail);
            request.Execute();
            return View("~/Views/Home/Index.cshtml");

        }

        public IActionResult Content_Body(string emailId)
        {
            string userEmail = User.FindFirst(u => u.Type == ClaimTypes.Email).Value;
            GmailService service = GetService();
            var emailInfoRequest = service.Users.Messages.Get(userEmail, emailId);

            // Make anothe request for that email id..
            emailInfoRequest.Format = UsersResource.MessagesResource.GetRequest.FormatEnum.Raw;

            Message result = emailInfoRequest.Execute();

            var body = result.Raw;

            byte[] data = Decode(body);
            string body1 = System.Text.Encoding.UTF8.GetString(data);
            ViewBag.body = body1;
            return View();
        }

        public byte[] Decode(string text)
        {
            string output = text;
            output = output.Replace('-', '+'); // 62nd char of encoding
            output = output.Replace('_', '/'); // 63rd char of encoding
            switch (output.Length % 4)
            { // Pad with trailing '='s
                case 0: break; // No pad chars in this case
                case 2: output += "=="; break; // Two pad chars
                case 3: output += "="; break;  // One pad char            
            }
            byte[] converted = Convert.FromBase64String(output); // Standard base64 decoder
            return converted;
        }

        string Encode(string text)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(text);
            return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').Replace("=", "");
        }
    }
}