using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BulkyWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles =SD.Role_Admin)]
    public class CategoryController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        public CategoryController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public IActionResult Index()
        {
            var objCategoryList = _unitOfWork.Category.GetAll().ToList();
            return View(objCategoryList);
        }
        public IActionResult Create()
        {
            return View();
        }
        [HttpPost]
        public IActionResult Create(Category obj)
        {

            if (ModelState.IsValid)
            {
                TempData["success"] = "Category Created Successfully";
                _unitOfWork.Category.Add(obj);
                _unitOfWork.Save();
                return RedirectToAction("Index");

            }
            return View();

        }
        public IActionResult Edit(int id)
        {
            if (id == 0 || id == null)
            {
                return NotFound();
            }
            Category categoryfromdb = _unitOfWork.Category.Get(u => u.Id == id);
            if (categoryfromdb == null)
            {
                return NotFound();
            }

            return View(categoryfromdb);
        }
        [HttpPost]
        public IActionResult Edit(Category obj)
        {


            if (ModelState.IsValid)
            {

                _unitOfWork.Category.Update(obj);
                _unitOfWork.Save();
                TempData["success"] = "Category Updated Successfully";
                return RedirectToAction("Index");

            }

            return View();

        }
        public IActionResult Delete(int id)
        {
            if (id == 0 || id == null)
            {
                return NotFound();
            }
            Category categoryfromdb = _unitOfWork.Category.Get(u => u.Id == id);
            if (categoryfromdb == null)
            {
                return NotFound();
            }
            return View(categoryfromdb);
        }
        [HttpPost]
        public IActionResult Delete(Category obj)
        {
            if (obj == null)
            {
                return NotFound();
            }

            _unitOfWork.Category.Remove(obj);
            _unitOfWork.Save();
            TempData["success"] = "Category Deleted Successfully";
            return RedirectToAction("Index");

        }

    }
}
