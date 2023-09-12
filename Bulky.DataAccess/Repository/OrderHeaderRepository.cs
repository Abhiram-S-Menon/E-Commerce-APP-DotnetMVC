using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bulky.DataAccess.Repository
{
    public class OrderHeaderRepository : Repository<OrderHeader>, IOrderHeaderRepository
    {
        private readonly ApplicationDbContext _db;
        public OrderHeaderRepository(ApplicationDbContext db):base(db)
        {
            _db = db;   
        }

        public void Update(OrderHeader obj)
        {
           _db.OrderHeaders.Update(obj);
        }

        public void UpdateStatus(int id, string orderStatus, string? paymentStatus = null)
        {
            var orderfromdb=_db.OrderHeaders.FirstOrDefault(x => x.Id == id);
            if(orderfromdb != null)
            {
                orderfromdb.OrderStatus = orderStatus;
                if (!string.IsNullOrEmpty(paymentStatus) )
                {
                    orderfromdb.PaymentStatus = paymentStatus;  
                }
            }
        }

        public void UpdateStripePaymentId(int id, string sessionId, string paymentIndentId)
        {
            var orderfromdb = _db.OrderHeaders.FirstOrDefault(x => x.Id == id);
            if(orderfromdb != null)
            {
                if(!string.IsNullOrEmpty(sessionId))
                {
                    orderfromdb.SessionId = sessionId;
                }
                if(!string.IsNullOrEmpty(paymentIndentId)) { }
                {
                    orderfromdb.PaymentIntentId = paymentIndentId;
                    orderfromdb.PaymentDate= DateTime.Now;

                }
            }
        }
    }
}
