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
    public class ProductController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnviroment;
        public ProductController(IUnitOfWork unitOfWork,IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _webHostEnviroment = webHostEnvironment;
        }
        public IActionResult Index()
        {
            var objProductList = _unitOfWork.Product.GetAll(includeProperties:"category").ToList();

            return View(objProductList);
        }
        public IActionResult Upsert(int ?id)
        {
            ProductVM productVM = new()
            {
                CategoryList = _unitOfWork.Category.GetAll().Select(u =>
                new SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id.ToString()
                }
                ),
                Product = new Product()
            };
            if(id==0||id==null)
            {

                return View(productVM);
            }
            else
            {
                productVM.Product = _unitOfWork.Product.Get(u => u.Id == id);
                return View(productVM);
            }

        }
        [HttpPost]
        public IActionResult Upsert(ProductVM obj,IFormFile? file)
        {

            if (ModelState.IsValid)
            {
                string wwwRootPath = _webHostEnviroment.WebRootPath;
                if(file!=null)
                {
                   
                    string filename=Guid.NewGuid().ToString()+Path.GetExtension(file.FileName);
                    string productPath=Path.Combine(wwwRootPath, @"images\product");

                    if(!string.IsNullOrEmpty(obj.Product.ImgUrl))
                    {
                        var oldimgpath = Path.Combine(wwwRootPath, obj.Product.ImgUrl.TrimStart('\\'));
                        if(System.IO.File.Exists(oldimgpath))
                        {
                            System.IO.File.Delete(oldimgpath);
                        }
                    }
                    using (var fileStream = new FileStream(Path.Combine(productPath, filename), FileMode.Create))
                    {
                        file.CopyTo(fileStream);
                    }
                    obj.Product.ImgUrl = @"\images\product\" + filename;

                }

                
                if (obj.Product.Id == 0)
                {
                    TempData["success"] = "Product Created Successfully";
                    _unitOfWork.Product.Add(obj.Product);
                }
                else
                {
                    TempData["success"] = "Product Updated Successfully";
                    _unitOfWork.Product.Update(obj.Product);
                }
                _unitOfWork.Save();
                return RedirectToAction("Index");

            }
            else
            {
                obj.CategoryList = _unitOfWork.Category.GetAll().Select(u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id.ToString()
                });
                return View(obj);
            }
        }
        #region APICALLS
        [HttpGet]
        public IActionResult GetAll()
        {
            var objProductList = _unitOfWork.Product.GetAll(includeProperties: "category").ToList();

            return Json(new { data = objProductList });
        }
        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            var productToBeDeleted = _unitOfWork.Product.Get(u => u.Id == id);
            if(productToBeDeleted==null)
            {
                return Json(new { success = false, message = "Error while deleting" });
            }

                var oldimgpath = Path.Combine(_webHostEnviroment.WebRootPath, productToBeDeleted.ImgUrl.TrimStart('\\'));
                if (System.IO.File.Exists(oldimgpath))
                {
                    System.IO.File.Delete(oldimgpath);
                }
                _unitOfWork.Product.Remove(productToBeDeleted);
            _unitOfWork.Save();

            return Json(new { success = true, message = "delete successfull" });

        }
        

        #endregion

    }
}
