using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models.ViewModels;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Stripe.Checkout;

namespace BulkyBookWeb.Areas.Customer.Controllers
{
	[Area("customer")]
	[Authorize]
	public class CartController : Controller
	{

		private readonly IUnitOfWork _unitOfWork;
		[BindProperty]
		public ShoppingCartVM shoppingCartVM { get; set; }
		public CartController(IUnitOfWork unitOfWork)
		{
			_unitOfWork = unitOfWork;
		}
		public IActionResult Index()
		{
			var claimsIdentity = (ClaimsIdentity)User.Identity;
			var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
			shoppingCartVM = new()
			{
				ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId,
				includeProperties: "Product"),
				OrderHeader = new()
			};
			foreach (var cart in shoppingCartVM.ShoppingCartList)
			{
				cart.price = GetPriceBasedOnQuantity(cart);
				shoppingCartVM.OrderHeader.OrderTotal += (cart.price * cart.Count);
			}
			return View(shoppingCartVM);
		}

		public IActionResult Summary()
		{
			var claimsIdentity = (ClaimsIdentity)User.Identity;
			var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
			shoppingCartVM = new()
			{
				ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId,
				includeProperties: "Product"),
				OrderHeader = new()
			};
			shoppingCartVM.OrderHeader.ApplicationUser = _unitOfWork.ApplicationUser.Get(u => u.Id == userId);
			shoppingCartVM.OrderHeader.Name = shoppingCartVM.OrderHeader.ApplicationUser.Name;
			shoppingCartVM.OrderHeader.PhoneNumber = shoppingCartVM.OrderHeader.ApplicationUser.PhoneNumber;
			shoppingCartVM.OrderHeader.StreetAddress = shoppingCartVM.OrderHeader.ApplicationUser.StreetAddress;
			shoppingCartVM.OrderHeader.City = shoppingCartVM.OrderHeader.ApplicationUser.City;
			shoppingCartVM.OrderHeader.State = shoppingCartVM.OrderHeader.ApplicationUser.State;
			shoppingCartVM.OrderHeader.PostalCode = shoppingCartVM.OrderHeader.ApplicationUser.PostalCode;
			foreach (var cart in shoppingCartVM.ShoppingCartList)
			{
				cart.price = GetPriceBasedOnQuantity(cart);
				shoppingCartVM.OrderHeader.OrderTotal += (cart.price * cart.Count);
			}
			return View(shoppingCartVM);
		}

		[HttpPost]
		[ActionName("Summary")]
		public IActionResult SummaryPOST()
		{
			var claimsIdentity = (ClaimsIdentity)User.Identity;
			var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
			shoppingCartVM = new()
			{
				ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId,
				includeProperties: "Product"),
				OrderHeader = new()
			};
			shoppingCartVM.OrderHeader.ApplicationUser = _unitOfWork.ApplicationUser.Get(u => u.Id == userId);
			shoppingCartVM.OrderHeader.Name = shoppingCartVM.OrderHeader.ApplicationUser.Name;
			shoppingCartVM.OrderHeader.PhoneNumber = shoppingCartVM.OrderHeader.ApplicationUser.PhoneNumber;
			shoppingCartVM.OrderHeader.StreetAddress = shoppingCartVM.OrderHeader.ApplicationUser.StreetAddress;
			shoppingCartVM.OrderHeader.City = shoppingCartVM.OrderHeader.ApplicationUser.City;
			shoppingCartVM.OrderHeader.State = shoppingCartVM.OrderHeader.ApplicationUser.State;
			shoppingCartVM.OrderHeader.PostalCode = shoppingCartVM.OrderHeader.ApplicationUser.PostalCode;
			shoppingCartVM.OrderHeader.OrderTotal = 0;
			foreach (var cart in shoppingCartVM.ShoppingCartList)
			{
				cart.price = GetPriceBasedOnQuantity(cart);
				shoppingCartVM.OrderHeader.OrderTotal += (cart.price * cart.Count);
			}

			shoppingCartVM.ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId,
				includeProperties: "Product");
			shoppingCartVM.OrderHeader.ApplicationUserId = userId;
			shoppingCartVM.OrderHeader.OrderDate = DateTime.Now;

			ApplicationUser applicationUser = _unitOfWork.ApplicationUser.Get(u => u.Id == userId);

            if (applicationUser.CompanyId.GetValueOrDefault() == 0)
            {
                //it is a regular customer 
                shoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
                shoppingCartVM.OrderHeader.OrderStatus = SD.StatusPending;
            }
            else
            {
                //it is a company user
                shoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusDelayedPayment;
                shoppingCartVM.OrderHeader.OrderStatus = SD.StatusApproved;
            }
            _unitOfWork.OrderHeader.Add(shoppingCartVM.OrderHeader);
			_unitOfWork.Save();
			foreach (var cart in shoppingCartVM.ShoppingCartList)
			{
				OrderDetail orderDetail = new()
				{
					ProductId = cart.ProductId,
					OrderHeaderId = shoppingCartVM.OrderHeader.Id,
					Price = cart.price,
					Count = cart.Count
				};
				_unitOfWork.OrderDetail.Add(orderDetail);
				_unitOfWork.Save();
			}

			if (applicationUser.CompanyId.GetValueOrDefault() == 0)
			{
				//it is a regular customer account and we need to capture payment
				//stripe logic
				var domain = "https://localhost:7151/";
				var options = new SessionCreateOptions
				{
					SuccessUrl = domain + $"customer/cart/OrderConfirmation?id={shoppingCartVM.OrderHeader.Id}",
					CancelUrl = domain + "customer/cart/index",
					LineItems = new List<SessionLineItemOptions>(),
					Mode = "payment",
				};

				foreach (var item in shoppingCartVM.ShoppingCartList)
				{
					var sessionLineItem = new SessionLineItemOptions
					{
						PriceData = new SessionLineItemPriceDataOptions
						{
							UnitAmount = (long)(item.price * 100), // $20.50 => 2050
							Currency = "inr",
							ProductData = new SessionLineItemPriceDataProductDataOptions
							{
								Name = item.Product.Title
							}
						},
						Quantity = item.Count
					};
					options.LineItems.Add(sessionLineItem);
				}


				var service = new SessionService();
				Session session = service.Create(options);
				_unitOfWork.OrderHeader.UpdateStripePaymentId(shoppingCartVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
				_unitOfWork.Save();
				Response.Headers.Add("Location", session.Url);
				return new StatusCodeResult(303);

			}
		
			return RedirectToAction(nameof(OrderConfirmation), new { id = shoppingCartVM.OrderHeader.Id });
		}

        public IActionResult OrderConfirmation(int id)
        {

            OrderHeader orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == id, includeProperties: "ApplicationUser");
            if (orderHeader.PaymentStatus != SD.PaymentStatusDelayedPayment)
            {
                //this is an order by customer

                var service = new SessionService();
                Session session = service.Get(orderHeader.SessionId);

                if (session.PaymentStatus.ToLower() == "paid")
                {
                    _unitOfWork.OrderHeader.UpdateStripePaymentId(id, session.Id, session.PaymentIntentId);
                    _unitOfWork.OrderHeader.UpdateStatus(id, SD.StatusApproved, SD.PaymentStatusApproved);
                    _unitOfWork.Save();
                }


            }

            List<ShoppingCart> shoppingCarts = _unitOfWork.ShoppingCart
                .GetAll(u => u.ApplicationUserId == orderHeader.ApplicationUserId).ToList();

            _unitOfWork.ShoppingCart.RemoveRange(shoppingCarts);
            _unitOfWork.Save();

            return View(id);
        }
        private double GetPriceBasedOnQuantity(ShoppingCart shoppingcart)
		{
			if (shoppingcart.Count <= 50)
			{
				return shoppingcart.Product.Price;
			}
			if (shoppingcart.Count <= 100)
			{
				return shoppingcart.Product.Price50;
			}
			else
			{
				return shoppingcart.Product.Price100;
			}
		}

		public IActionResult Plus(int cartId)
		{
			var cartfromdb = _unitOfWork.ShoppingCart.Get(u => u.Id == cartId);
			cartfromdb.Count += 1;
			_unitOfWork.ShoppingCart.Update(cartfromdb);
			_unitOfWork.Save();
			return RedirectToAction(nameof(Index));
		}
		public IActionResult Minus(int cartId)
		{
			var cartfromdb = _unitOfWork.ShoppingCart.Get(u => u.Id == cartId);
			cartfromdb.Count -= 1;
			if (cartfromdb.Count < 1)
			{
				_unitOfWork.ShoppingCart.Remove(cartfromdb);
			}
			else
			{
				_unitOfWork.ShoppingCart.Update(cartfromdb);
			}
			_unitOfWork.Save();
			return RedirectToAction(nameof(Index));
		}
		public IActionResult Remove(int cartId)
		{
			var cartfromdb = _unitOfWork.ShoppingCart.Get(u => u.Id == cartId);
			_unitOfWork.ShoppingCart.Remove(cartfromdb);
			_unitOfWork.Save();
			return RedirectToAction(nameof(Index));

		}
	}
}
