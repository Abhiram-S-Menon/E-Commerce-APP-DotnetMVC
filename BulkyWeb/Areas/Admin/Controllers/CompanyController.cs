using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Data;
using System.IO;

namespace BulkyWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class companyController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
       
        public companyController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
           
        }
        public IActionResult Index()
        {
            var objcompanyList = _unitOfWork.Company.GetAll().ToList();

            return View(objcompanyList);
        }
        public IActionResult Upsert(int? id)
        {

            if (id == 0 || id == null)
            {

                return View(new Company());
            }
            else
            {
                Company company = _unitOfWork.Company.Get(u => u.Id == id);
                return View(company);
            }

        }
        [HttpPost]
        public IActionResult Upsert(Company obj)
        {

            if (ModelState.IsValid)
            {
                


                if (obj.Id == 0)
                {
                    TempData["success"] = "company Created Successfully";
                    _unitOfWork.Company.Add(obj);
                }
                else
                {
                    TempData["success"] = "company Updated Successfully";
                    _unitOfWork.Company.Update(obj);
                }
                _unitOfWork.Save();
                return RedirectToAction("Index");

            }
            else
            {
                return View(obj);
            }
        }
        #region APICALLS
        [HttpGet]
        public IActionResult GetAll()
        {
            var objcompanyList = _unitOfWork.Company.GetAll().ToList();

            return Json(new { data = objcompanyList });
        }
        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            var companyToBeDeleted = _unitOfWork.Company.Get(u => u.Id == id);
            if (companyToBeDeleted == null)
            {
                return Json(new { success = false, message = "Error while deleting" });
            }
            _unitOfWork.Company.Remove(companyToBeDeleted);
            _unitOfWork.Save();

            return Json(new { success = true, message = "delete successfull" });

        }


        #endregion

    }
}
