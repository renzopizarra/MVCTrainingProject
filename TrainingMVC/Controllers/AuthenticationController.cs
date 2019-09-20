using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using TrainingMVC.Models;

namespace TrainingMVC.Controllers
{
    public class AuthenticationController : Controller
    {
      [HttpGet]
      public ActionResult Registration()
        {
            if (Session["Email"] != null)  //Session verification
            {
                return RedirectToAction("Index", "Employee"); //If not null redirect to home page
            }
            return View(); //If not, just return the login view
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        //Exclude the two fields for registration
        public ActionResult Registration([Bind(Exclude = "IsEmailVerified,ActivationCode")] User user) 
        {
            bool Status = false;
            string message = "";

            //Model Validation
            //If all the input fields is valid
            if (ModelState.IsValid) 
            {
                #region "IF Email already exist"
                //Validate if email already exist
                var emailexist = IsEmailExist(user.Email);
                if (emailexist)
                {
                    //Add error to my ValidationMessage named "EmailExist" and add error message."
                    ModelState.AddModelError("EmailExist", "Email already exist.");
                    return View(user);
                }
                #endregion

                #region "Generate activation code"
                //Generate a Global Unique Identifier or GUID for email activation code
                user.ActivationCode = Guid.NewGuid();
                #endregion

                #region "Hash Password"
                //Password and confirm password Hashing
                user.Password = PasswordHash.Hash(user.Password).ToUpper();
                user.ConfirmPassword = PasswordHash.Hash(user.ConfirmPassword).ToUpper();
                #endregion

                //Set EmailVerified to false which it's equivalent in our SSMS boolean datataype is "0"
                user.IsEmailVerified = false;

                #region "Save to database"
                //The finally, save to database.
                using (EmployeeDBEntitiesEntities db = new EmployeeDBEntitiesEntities())
                {
                    db.Users.Add(user);
                    db.SaveChanges();

                    //Send email to user for account activation
                    SendEmailVerification(user.Email, user.ActivationCode.ToString());
                    message = "Successfully registered! Please go to your email to activate you account: " + user.Email;
                    //Set status to true to enable Success Message on our view
                    Status = true;
                }
                #endregion

            }
            else
            {
                message = "Invalid Request";
            }
            ViewBag.Message = message;
            ViewBag.Status = Status;
            return View(user);
        }

        //If Account is Verified
        [HttpGet]
        public ActionResult VerifyAccount(string id)
        {
            bool Status = false;
            using (EmployeeDBEntitiesEntities db = new EmployeeDBEntitiesEntities())
            {
                //Gets or sets a value indicating whether tracked entities should be validated automatically
                //when SaveChanges() is invoked.
                //The default value is true
                //Temporarily turn off the model validation by setting ValidateOnSaveEnabled of context to false
                db.Configuration.ValidateOnSaveEnabled = false;
                //Verify if Activation Code is equal to the GUID or Global Unique Identifier of our string
                var x = db.Users.Where(model => model.ActivationCode == new Guid(id)).FirstOrDefault();
                if (x != null)
                {
                    //if value of x is not null, then update IsEmailVerified to true or 1 in our database
                    x.IsEmailVerified = true;
                    db.SaveChanges();
                    Status = true;
                }
                else
                {
                    ViewBag.Message = "Invalid request";
                }
            }
            ViewBag.Status = Status;
            return View();
        }

        //REturn view of Login
        [HttpGet]
        public ActionResult Login()
        {
            //Check if session is not null
            if (Session["Email"] != null)
            {
                //If not null, then redirect to our home page
                return RedirectToAction("Index", "Employee");
            }
            
            return View();
        }

        //Login Post
        //restrict all action method so that method only handless HTT POST requests
        [HttpPost]
        //To prevent forgery of request on our method
        [ValidateAntiForgeryToken]
        public ActionResult Login(UserLogin login, string ReturnURL = "")
        {
            string message = "";
            //Verify if email already verified
            if (VerifiedEmail(login.Email) == true) {

                using (EmployeeDBEntitiesEntities db = new EmployeeDBEntitiesEntities())
                {
                    var x = db.Users.Where(model => model.Email == login.Email).FirstOrDefault();
                    //if db context contains the input email
                    if (x != null)
                    {
                        //Hash the model password then compare if to the db context password
                        if (string.Compare(PasswordHash.Hash(login.Password).ToUpper(), x.Password) == 0)
                        {
                            //**Pluralsight demo
                            //Set ClaimsIdentity which Authetication type is "ApplicationCookie"
                            var identity = new ClaimsIdentity("ApplicationCookie");
                            identity.AddClaims(new List<Claim>
                            {
                                //Add the value for ApplicationCookie if email and password is valid
                                new Claim(ClaimTypes.NameIdentifier, login.Email),
                                new Claim(ClaimTypes.Name, login.Email)
                            });
                            //Sign in identity
                            HttpContext.GetOwinContext().Authentication.SignIn(identity);

                            //**If remember me is clicked
                            //1800s = 30 mins
                            int timeout = login.RememberMe ? 1800 : 20;
                            var ticket = new FormsAuthenticationTicket(login.Email, login.RememberMe, timeout);
                            string encrpyt = FormsAuthentication.Encrypt(ticket);
                            var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, encrpyt);
                            cookie.Expires = DateTime.Now.AddMinutes(timeout);
                            // cookie not available in javascript
                            cookie.HttpOnly = true; 
                            
                            Response.Cookies.Add(cookie);
                            if (Url.IsLocalUrl(ReturnURL))
                            {
                                return Redirect(ReturnURL);
                            }
                            else
                            {
                                //Add Session
                                Session["Email"] = login.Email;
                                return RedirectToAction("Index", "Employee");
                            }
                        }
                        else
                        {
                            message = "Invalid Email or Password.";
                        }
                    }
                    else
                    {
                        message = "Invalid Email or Password.";
                    }
                }

                ViewBag.Message = message;
                return View();
            }
            else
            {
                //If email is not yet activated
                message = "You account is not yet activated.";
                ViewBag.Message = message;
            }

            return View();

        }


        //FacebookLogin

        //    public ActionResult LoginFacebook()
        //{

        //    HttpContext.GetOwinContext().Authentication.Challenge(new Microsoft.Owin.Security.AuthenticationProperties
        //    {
        //        RedirectUri = "/Employee/Index"
        //    }, "Facebook");
        //    return new HttpUnauthorizedResult();
        //}

        //Logout
        [Authorize]
        [HttpPost]
        public ActionResult Logout()
        {
            //Remove the forms authentication ticket from the browser
            FormsAuthentication.SignOut();
            //Cancel current Session
            Session.Abandon();
            //Then using GetOwinContext, set authentication to signout
            HttpContext.GetOwinContext().Authentication.SignOut();
            //Then after signout, redirect user to Login view.
            return RedirectToAction("Login", "Authentication");
        }

        //IF Forgot Password
        [HttpGet]
        public ActionResult ForgotPassword()
        {
            return View();
        }
        [HttpPost]
        public ActionResult ForgotPassword(string Email)
        {
            string message = "";
            bool status = false;

            using (EmployeeDBEntitiesEntities db = new EmployeeDBEntitiesEntities())
            {
                //using our db context, verify if input email exists
                var account = db.Users.Where(model => model.Email == Email).FirstOrDefault();
                if (account != null)
                {
                    //Send email for resetting password
                    //Sending new Global Unique Identifier
                    string resetCode = Guid.NewGuid().ToString();
                    //Method for sending email verification, code for reset passowrd, then the view.
                    SendEmailVerification(account.Email, resetCode, "ResetPassword");
                    account.ResetPasswordCode = resetCode;
                    //Temporarily turn off the model validation
                    db.Configuration.ValidateOnSaveEnabled = false;
                    db.SaveChanges();
                    //Clear input field
                    ModelState.Clear();
                    status = true;
                    message = "Reset password link has been sent to your email.";
                }
                else
                {
                    message = "Account not found";
                }
            }
            ViewBag.Status = status;
            ViewBag.Message = message;
            return View();
        }

        public ActionResult ResetPassword(string id)
        {
            bool status = false;
            using (EmployeeDBEntitiesEntities db = new EmployeeDBEntitiesEntities())
            {
                var userpass = db.Users.Where(model => model.ResetPasswordCode == id).FirstOrDefault();
                //Validate if reset password code is not null
                if (userpass != null)
                {
                    //Model for New Password, confirm password and resetcode
                    ResetPasswordModel model = new ResetPasswordModel();
                    //Pass resetpasswordcode from db to reset code
                    model.ResetCode = id;
                    status = true;
                    return View(model);
                }
                else
                {
                    ViewBag.Status = status;
                    //if useprass is not valid, set httpnotfoundresult class
                    return HttpNotFound();
                }
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ResetPassword(ResetPasswordModel model)
        {
            var message = "";
            //Verify if there's no error
            if (ModelState.IsValid)
            {
                using (EmployeeDBEntitiesEntities db = new EmployeeDBEntitiesEntities())
                {
                    //Check if ResetPasswordCode is equal to Reset Code
                    var user = db.Users.Where(a => a.ResetPasswordCode == model.ResetCode).FirstOrDefault();
                    //If var user reset pass code is not null
                    if (user != null)
                    {
                        //Hash new password
                        user.Password = PasswordHash.Hash(model.NewPassword).ToUpper();
                        //Clear now ResetPasswordCode 
                        user.ResetPasswordCode = "";
                        db.Configuration.ValidateOnSaveEnabled = false;
                        //Save changes
                        db.SaveChanges();
                        message = "Your password updated successfully.";
                    }
                }
            }
            else
            {
                message = "You password is invalid";
            }

            ViewBag.Message = message;
            return View(model);
        } 


        //Verify if Email Already Exist
        [NonAction]
        public Boolean IsEmailExist(string email)
        {
            using (EmployeeDBEntitiesEntities db = new EmployeeDBEntitiesEntities())
            {
                //Check db context if input email is already exists.
                var x = db.Users.Where(model => model.Email == email).FirstOrDefault();
                return x != null;
            }
        }

        //Send Email Activation
        //This method is using email verification and reset password confirmation
        [NonAction]
        public void SendEmailVerification(string email, string activationCode, string emailFor= "VerifyAccount")
        {
            //Activation code value is from db
            var verifyurl = "/Authentication/" + emailFor + "/" + activationCode;
            var link = Request.Url.AbsoluteUri.Replace(Request.Url.PathAndQuery, verifyurl);
            //Created a sample email account that will send email messages.
            var fromEmail = new MailAddress("renzopizarra.mvc@gmail.com", "Renzo Pizarra MVC");
            var toEmail = new MailAddress(email);
            var fromEmailPassword = "P@ssw0rd!@#";
            string subject = "";
            string body = "";

            //This message is for Verifying account or email confirmation
            if (emailFor == "VerifyAccount")
            {
                subject = "Renzo Pizarra MVC Email Verification";
                 body = "<br /><strong><h2>VERIFY EMAIL ADDRESS</h2></strong><br /><br />" +
               "To finish setting up your Renzo Pizarra MVC account, we just need to make sure this email address is yours. <br /><br />" +
               "To verify your email address, please click the link below<br /><br />" +
               "<a href='" + link + "'>Verify Email</a> <br /><br />" +
               "If you didn't request this email verification, you can safely ignore this email." +
               "Someone else might have typed your email address by mistake.<br /> <br /><br />" +
               "Thanks,<br/>" +
               "The Renzo Pizarra MVC Team";
            }
            //Below is for resetting password
            else if (emailFor=="ResetPassword")
            {
                subject = "Renzo Pizarra MVC Account Recovery";
                body = "<br /><strong><h2>ACCOUNT RECOVERY</h2></strong><br /><br />" +
                    "We received a request to reset your Renzo Pizarra MVC password.<br /><br />" +
                    "To reset your password, please click the link below<br /><br />" +
                   "<a href='" + link + "'>Reset Password</a> <br /><br />" +
                   "If you didn't request this email verification, you can safely ignore this email." +
                   "Someone else might have typed your email address by mistake.<br /> <br /><br />" +
                   "Thanks,<br/>" +
                   "The Renzo Pizarra MVC Team";
            }

            //Setting up SMPT or Simple Mail Transfer Protocol
            //This will allow our web app to send emails
            var smtp = new SmtpClient
            {
                //because or email account is @gmail
                Host = "smtp.gmail.com",
                //port number of smpt.gmail
                Port = 587,
                //enable secure socket layers to encrypt our connection
                EnableSsl = true,
                //The message is sent via the network to the SMTP server
                DeliveryMethod = SmtpDeliveryMethod.Network,
                //Set use fault credentials so that we can use the credentials i set from above.
                UseDefaultCredentials = false,
                //Use the credentials we set.
                Credentials = new NetworkCredential(fromEmail.Address, fromEmailPassword)
            };
            
            //Compose now the message
            using (var message = new MailMessage(fromEmail, toEmail)
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            })
                //then send
                smtp.Send(message);


        }

        //this represents the this attribute is used to indicate that this method
        //is not an action method
        [NonAction]
        //Boolean method if email is verified or not
        public bool VerifiedEmail(string email)
        {
            //bool Status = false;
            //string message = "";
            using (EmployeeDBEntitiesEntities db = new EmployeeDBEntitiesEntities())
            {
                db.Configuration.ValidateOnSaveEnabled = false;
                var x = db.Users.Where(model => model.Email == email).FirstOrDefault();
                if (x != null)
                {
                    if (x.IsEmailVerified == false)
                    {
                       //Return false if not yet verified
                        return false;
                    }
                }
                else
                {
                    ViewBag.Message = "Invalid request";
                }
            }
            //Else return true
            return true;
        }





    }
}