using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ApiGateway.Filters;

public class ValidatePdfAttribute : ActionFilterAttribute
{
	public override void OnActionExecuting(ActionExecutingContext context)
	{
		var file = context.ActionArguments.Values.OfType<IFormFile>().FirstOrDefault();

		if(file == null)
		{
			context.Result = new BadRequestObjectResult(new { Error = "No file uploaded" });
			return;
		}

		if(file.Length == 0)
		{
			context.Result = new BadRequestObjectResult(new { Error = "File is empty" });
			return;
		}

		var contentType = file.ContentType?.ToLower();
		if(contentType != "application/pdf" && contentType != "application/x-pdf")
		{
			context.Result = new BadRequestObjectResult(new { Error = "Only PDF files are allowed" });
			return;
		}

		var extension = Path.GetExtension(file.FileName)?.ToLower();
		if(extension != ".pdf")
		{
			context.Result = new BadRequestObjectResult(new { Error = "File must have .pdf extension" });
			return;
		}
	}
}