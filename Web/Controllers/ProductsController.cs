using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Web.Models;

namespace Web.Controllers
{
    public class ProductsController : Controller
    {
        //
        // GET: /Products/
        public ActionResult Index()
        {
            
          
          return View(DB.Products);
        }

        //
        // GET: /Products/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        //
        // GET: /Products/Create
        public ActionResult Create()
        {
          return View(new Product());
        }

        //
        // POST: /Products/Create
        [HttpPost]
        public ActionResult Create(Product product)
        {
            // TODO: Add insert logic here
            DB.Products.Add(product);
            //DB.Products.Save();
            return RedirectToAction("Index");

        }

        //
        // GET: /Products/Edit/5
        public ActionResult Edit(string id)
        {
          var product = DB.Products.FirstOrDefault(x => x.Sku == id);
          return View(product);
        }

        //
        // POST: /Products/Edit/5
        [HttpPost]
        public ActionResult Edit(string id, Product p)
        {
            // TODO: Add update logic here
            var product = DB.Products.FirstOrDefault(x => x.Sku == id);
            DB.Products.Update(p);
            //DB.Products.Save();
            return RedirectToAction("Index");

        }

        //
        // GET: /Products/Delete/5
        public ActionResult Delete(string id)
        {
          var product = DB.Products.FirstOrDefault(x => x.Sku == id);
          return View(product);
        }

        //
        // POST: /Products/Delete/5
        [HttpPost]
        public ActionResult Delete(string id, FormCollection form)
        {
            try
            {
                // TODO: Add delete logic here
                var product = DB.Products.FirstOrDefault(x => x.Sku == id);
                DB.Products.Remove(product);
                //DB.Products.Save();
                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }
    }
}
