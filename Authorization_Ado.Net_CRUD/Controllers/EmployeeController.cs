using Authorization_Ado.Net_CRUD.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Authorization_Ado.Net_CRUD.Controllers
{
    public class EmployeeController : Controller
    {
        private static string connectionString = "";
        private readonly IAuthenticationService _authenticationService;

        public EmployeeController(IConfiguration configuration, IAuthenticationService authenticationService)
        {
            connectionString = configuration.GetConnectionString("Con");
            _authenticationService = authenticationService;
        }
        [HttpGet]
        [Authorize]

        public IActionResult Index()
        {
            List<Employee> employees = GetAllAsync();

            return View(employees);
        }
        public List<Employee> GetAllAsync()
        {
            List<Employee> employees = new List<Employee>();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlCommand command = new SqlCommand("sp_GetAllEmps", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    connection.Open();
                    command.Parameters.AddWithValue("@ActionName", "Select");
                    SqlDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        Employee employee = new Employee();
                        employee.Id = Convert.ToInt32(reader["Id"]);
                        employee.Name = reader["Name"].ToString();
                        employee.Address = reader["Address"].ToString(); 
                        employee.Salary = reader["Salary"].ToString();
                        employees.Add(employee);
                    }
                    reader.Close();
                }
            }
            return employees;
        }
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Login(LoginUser user)
        {
            if (ModelState.IsValid)
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_LoginUser", connection))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        connection.Open();
                        cmd.Parameters.AddWithValue("@Password", user.Password);
                        cmd.Parameters.AddWithValue("@UserName", user.UserName);
                        cmd.Parameters.AddWithValue("@Action", "login");
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var claims = new List<Claim>
                                {
                                     new Claim(ClaimTypes.Name, reader["UserName"].ToString()),
                                  new Claim(ClaimTypes.Role,  reader["Role"].ToString()),

                                };
                                var claimsIdentity = new ClaimsIdentity(
                            claims, CookieAuthenticationDefaults.AuthenticationScheme);
                                var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
                                _authenticationService.SignInAsync(HttpContext, CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal, new AuthenticationProperties
                                { IsPersistent = false,
                                    // ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(15)
                                    ExpiresUtc = DateTimeOffset.UtcNow.AddSeconds(15)
                                }); 
                                return RedirectToAction("Index", "Home");
                            }
                        }
                    }
                }
            }
            ModelState.AddModelError("", "Invalid login attempt");
            return View(user);
        }

        [HttpPost]
        public ActionResult Logout()
        {
            HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Employee");
        }

    }
}
