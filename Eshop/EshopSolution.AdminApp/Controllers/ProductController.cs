﻿using EshopSolution.ApiIntergate;
using EshopSolution.ViewModels.Catalog.Categories;
using EshopSolution.ViewModels.Catalog.Products;
using EshopSolution.ViewModels.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EshopSolution.AdminApp.Controllers
{
    [Authorize(Policy = "Edit")]
    public class ProductController : BaseController
    {
        private readonly IProductApiClient _productApiClient;
        private readonly ICategoryApiClient _categoryApiClient;
        private readonly ILanguageApiClient _languageApiClient;

        public ProductController(IProductApiClient productApiClient, ICategoryApiClient categoryApiClient,
           ILanguageApiClient languageApiClient)
        {
            _productApiClient = productApiClient;
            _categoryApiClient = categoryApiClient;
            _languageApiClient = languageApiClient;
        }

        public async Task<IActionResult> Index(string keyword, int? categoryId, int pageIndex = 1, int pageSize = 5)
        {
            var request = new ProductPagingRequest()
            {
                Keyword = keyword,
                PageIndex = pageIndex,
                PageSize = pageSize,
                LanguageId = GetLanguageId(),
            };

            if (categoryId != null)
                request.CategoryId = (int)categoryId;
            ViewBag.Keyword = keyword;
            List<CategoryViewModel> categories = await _categoryApiClient.GetAll(GetLanguageId());
            ViewBag.Categories = categories.Select(x => new SelectListItem()
            {
                Text = x.Name,
                Value = x.Id.ToString(),
                Selected = categoryId.HasValue && categoryId.Value == x.Id
            });

            if (TempData["Result"] != null)
            {
                ViewBag.SuccessMsg = TempData["Result"];
            }
            var data = await _productApiClient.GetProductPaging(request);
            return View(data.ResultObj);
        }

        [HttpGet]
        public async Task<IActionResult> CreateAsync()
        {
            ViewBag.Languages = (await _languageApiClient.GetAll()).ResultObj;
            var productCreateRequest = new ProductCreateRequest()
            {
                LanguageId = GetLanguageId()
            };
            return View(productCreateRequest);
        }

        [HttpPost]
        public async Task<IActionResult> Create(ProductCreateRequest request)
        {
            if (!ModelState.IsValid)
            {
                return View(request);
            }
            //request.LanguageId = GetLanguageId();
            var result = await _productApiClient.Create(request);
            if (result.IsSuccessed)
            {
                TempData["Result"] = "Tạo mới thành công";
                return RedirectToAction("Index", "Product");
            }
            ModelState.AddModelError("", result.Message);
            return View(request);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }
            var result = await _productApiClient.GetById(id, GetLanguageId());

            if (result.IsSuccessed)
            {
                var updateRequest = new ProductUpdateRequest()
                {
                    Id = id,
                    Name = result.ResultObj.Name,
                    Description = result.ResultObj.Description,
                    Details = result.ResultObj.Details,
                    SeoDescription = result.ResultObj.SeoDescription,
                    SeoTitle = result.ResultObj.SeoTitle,
                    LanguageId = result.ResultObj.LanguageId,
                    SeoAlias = result.ResultObj.SeoAlias,
                    isFeatured = result.ResultObj.IsFeatured
                };
                //updateRequest.ThumbnailImage. = result.ResultObj.ThumbnailImage;

                ViewBag.Languages =(await _languageApiClient.GetAll()).ResultObj;
                return View(updateRequest);
            }
            return RedirectToAction("Error", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> Edit(ProductUpdateRequest request)
        {
            if (!ModelState.IsValid)
            {
                return View(request);
            }
            if (request.LanguageId == null)
                request.LanguageId = GetLanguageId();
            var result = await _productApiClient.Update(request.Id, request);

            if (result.IsSuccessed)
            {
                TempData["Result"] = "Chỉnh sửa thành công";
                return RedirectToAction("Index", "Product");
            }
            ModelState.AddModelError("", result.Message);
            return View(request);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }
            var result = await _productApiClient.GetById(id, GetLanguageId());

            if (result.IsSuccessed)
            {
                return View(result.ResultObj);
            }
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int Id)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }
            var result = await _productApiClient.GetById(Id, GetLanguageId());

            if (result.IsSuccessed)
            {
                return View(new ProductDeleteRequest(Id));
            }

            return RedirectToAction("Error", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(ProductDeleteRequest request)
        {
            if (!ModelState.IsValid)
            {
                return View(request.Id);
            }
            var result = await _productApiClient.Delete(request.Id);
            if (result.IsSuccessed)
            {
                TempData["Result"] = "Xóa thành công";
                return RedirectToAction("Index", "Product");
            }
            ModelState.AddModelError("", result.Message);
            return View(request.Id);
        }

        [HttpGet]
        public async Task<IActionResult> CategoryAssign(int id)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }
            var categoryAssign = await GetCategoryAssignRequest(id);
            return View(categoryAssign);
        }

        [HttpPost]
        public async Task<IActionResult> CategoryAssign(CategoryAssignRequest request)
        {
            if (!ModelState.IsValid)
            {
                return View(request);
            }
            var result = await _productApiClient.CategoryAssign(request.Id, request);
            if (result.IsSuccessed)
            {
                TempData["Result"] = "Cập nhập danh mục thành công";
                return RedirectToAction("Index", "Product");
            }
            ModelState.AddModelError("", result.Message);
            var categoryAssignRequest = await GetCategoryAssignRequest(request.Id);
            return View(categoryAssignRequest);
        }

        private async Task<CategoryAssignRequest> GetCategoryAssignRequest(int id)
        {
            string languageId = GetLanguageId();
            var productViewModel = (await _productApiClient.GetById(id, languageId)).ResultObj;
            var categories = (await _categoryApiClient.GetAll(languageId));
            var categoryAssignRequest = new CategoryAssignRequest();

            foreach (var category in categories)
            {
                categoryAssignRequest.Categories.Add(new SelectedItem()
                {
                    Id = category.Id.ToString(),
                    Name = category.Name,
                    Selected = productViewModel.Categories.Contains(category.Name)
                });
            }
            return categoryAssignRequest;
        }

    }
}