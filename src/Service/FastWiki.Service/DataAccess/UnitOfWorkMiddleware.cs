namespace FastWiki.Service.DataAccess;

public class UnitOfWorkMiddleware(ILogger<UnitOfWorkMiddleware> logger) : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        // 如果是Get请求，直接跳过
        if (context.Request.Method == "GET")
        {
            await next(context);

            return;
        }

        try
        {
            // 获取工作单元
            var unitOfWork = context.RequestServices.GetServices<IUnitOfWork>();

            await next(context);

            foreach (var work in unitOfWork)
            {
                await work.SaveChangesAsync();
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
        }
    }
}