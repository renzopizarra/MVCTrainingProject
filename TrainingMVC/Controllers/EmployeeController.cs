using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TrainingMVC.Models;

namespace TrainingMVC.Controllers
{
    public class EmployeeController : Controller
    {
        //Specify that any action or method is restricted to users who meet the authorization reqiurement
        [Authorize]
        public ActionResult Index()
        {
            //Verifey Session if null
            if (Session["Email"] == null)
            {
                return RedirectToAction("Login", "Authentication");
            }
            return View();
        }

        public ActionResult GetData()
        {
            //Retrieve now the data from the database
            using (EmployeeDBEntitiesEntities db = new EmployeeDBEntitiesEntities())
            {
                //Create now the an employee list 
                //return db.employees list to emplist
                List<Employee> empList = db.Employees.ToList<Employee>();
                //return a json or JavaScript Object Notation view 
                //allow http get requests
                //pass emplist to json data.
                return Json(new { data = empList }, JsonRequestBehavior.AllowGet);
            }

        }

        public ActionResult CurrentUser(string loggeduser)
        {
            
             if (Session["Email"] == null)
                {
                    return RedirectToAction("Login", "Authentication");
                }
                else
                {
                    using (EmployeeDBEntitiesEntities db = new EmployeeDBEntitiesEntities())
                    {
                    loggeduser = Session["Email"].ToString();
                        return View(db.Users.Where(x => x.Email == loggeduser).FirstOrDefault<User>());
                    }
                //List<User> userList = db.Users.ToList<User>();
                //return Json(new { data = userList }, JsonRequestBehavior.AllowGet);
            }
                
            }
        

        [HttpGet]
        public ActionResult AddOrEdit(int id = 0)
        {
            //If sessions is null, redirect to login
            if (Session["Email"] == null)
            {
                return RedirectToAction("Login", "Authentication");
            }
            //if not,
            //if id = 0, retrieve employee table
            if (id == 0)
                return View(new Employee());
            else
            {
                using (EmployeeDBEntitiesEntities db = new EmployeeDBEntitiesEntities())
                {
                    //if not, id = 0, retrieve the corresponding row with corresponding id
                    return View(db.Employees.Where(x => x.Id == id).FirstOrDefault<Employee>());
                }
            }

        }

        [HttpPost]
        public ActionResult AddOrEdit(Employee emp)
        {
            using (EmployeeDBEntitiesEntities db = new EmployeeDBEntitiesEntities())
            {
                //use for adding and updating new employee
                if (emp.Id == 0)
                {
                    //add the values from employee model, to employees table
                    db.Employees.Add(emp);
                    //save db changes
                    db.SaveChanges();
                    //return now the view, with success message for adding successfully.
                    return Json(new { success = true, message = "Saved successfully", JsonRequestBehavior.AllowGet });
                }
                else
                {
                    //update entry, set db entity state to modified
                    db.Entry(emp).State = EntityState.Modified;
                    //save changes
                    db.SaveChanges();
                    //return view with success message
                    return Json(new { success = true, message = "Updated successfully", JsonRequestBehavior.AllowGet });
                }
            }

        }

        [HttpPost]
        public ActionResult Delete(int id)
        {
            using (EmployeeDBEntitiesEntities db = new EmployeeDBEntitiesEntities())
            {
                //find the id of the row you want to delete.
                Employee emp = db.Employees.Where(x => x.Id == id).FirstOrDefault<Employee>();
                //remove
                db.Employees.Remove(emp);
                //then save changes from the db
                db.SaveChanges();
                //return view with success message
                return Json(new { success = true, message = "Deleted successfully", JsonRequestBehavior.AllowGet });
            }
        }
    }
}