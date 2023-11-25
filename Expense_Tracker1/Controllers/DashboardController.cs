using Expense_Tracker1.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Expense_Tracker1.Controllers
{
    public class DashboardController : Controller
    {

        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ActionResult> Index()
        {
            try
            {
                //Last 7 Days
                DateTime StartDate = DateTime.Today.AddDays(-6);
                DateTime EndDate = DateTime.Today;

                List<Transaction> SelectedTransactions = await _context.Transactions
                    .Include(x => x.Category)
                    .Where(y => y.Date >= StartDate && y.Date <= EndDate)
                    .ToListAsync();



                ////Total Income
                //decimal TotalIncome = SelectedTransactions
                //    .Where((Transaction i) => i.Category.Type == "Income")
                //    .Sum((Transaction j) => j.Amount);
                //ViewBag.TotalIncome = TotalIncome.ToString("C0");
                // Before calculating TotalIncome, set a breakpoint
                var incomeTransactions = SelectedTransactions.Where((Transaction i) => i.Category.Type == "Income").ToList();

                // Calculate TotalIncome directly from incomeTransactions
                decimal TotalIncome = incomeTransactions.Sum((Transaction j) => j.Amount);
                ViewBag.TotalIncome = TotalIncome.ToString("C0");

                //Total Expense
                decimal TotalExpense = SelectedTransactions
                    .Where((Transaction i) => i.Category.Type == "Expense")
                    .Sum((Transaction j) => j.Amount);
                ViewBag.TotalExpense = TotalExpense.ToString("C0");

                //Balance
                decimal Balance = TotalIncome - TotalExpense;

                // Formatting currency based on Balance
                string formattedBalance = Balance >= 0
                    ? Balance.ToString("C0", CultureInfo.CurrentCulture)
                    : Balance.ToString("C0", CultureInfo.GetCultureInfo("en-US"));

                ViewBag.Balance = formattedBalance;

                //Doughnut Chart - Expense By Category
                ViewBag.DoughnutChartData = SelectedTransactions
               .Where((Transaction i) => i.Category.Type == "Expense")
                 .GroupBy((Transaction j) => j.Category.CategoryId)
                 .Select((IGrouping<int, Transaction> k) => new DoughnutChartDataItem
                 {
                    CategoryTitleWithIcon = k.First().Category.Icon + " " + k.First().Category.Title,
                     Amount = k.Sum(j => j.Amount),
                     FormattedAmount = k.Sum(j => j.Amount).ToString("C0"),
                 })
                     .OrderByDescending((DoughnutChartDataItem l) => l.Amount)
                     .ToList();

                //Spline Chart - Income vs Expense

                //Income
                List<SplineChartData> IncomeSummary = SelectedTransactions
                    .Where((Transaction i) => i.Category.Type == "Income")
                    .GroupBy((Transaction j) => j.Date)
                    .Select((IGrouping<DateTime, Transaction> k) => new SplineChartData()
                    {
                        day = k.First().Date.ToString("dd-MMM"),
                        income = k.Sum(l => l.Amount)
                    })
                    .ToList();

                //Expense
                List<SplineChartData> ExpenseSummary = SelectedTransactions
                    .Where((Transaction i) => i.Category.Type == "Expense")
                    .GroupBy((Transaction j) => j.Date)
                    .Select((IGrouping<DateTime, Transaction> k) => new SplineChartData()
                    {
                        day = k.First().Date.ToString("dd-MMM"),
                        expense = k.Sum(l => l.Amount)
                    })
                    .ToList();

                //Combine Income & Expense
                string[] Last7Days = Enumerable.Range(0, 7)
                    .Select((int i) => StartDate.AddDays(i).ToString("dd-MMM"))
                    .ToArray();

                ViewBag.SplineChartData = from day in Last7Days
                                          join income in IncomeSummary on day equals income.day into dayIncomeJoined
                                          from income in dayIncomeJoined.DefaultIfEmpty(new SplineChartData { day = day }) // Providing a default value in case of null
                                          join expense in ExpenseSummary on day equals expense.day into expenseJoined
                                          from expense in expenseJoined.DefaultIfEmpty(new SplineChartData { day = day }) // Providing a default value in case of null
                                          select new
                                          {
                                              day = day,
                                              income = income?.income ?? 0, // Handling potential null reference for income
                                              expense = expense?.expense
                                          };
                                //Recent Transactions
                ViewBag.RecentTransactions = await _context.Transactions
                    .Include((Transaction i) => i.Category)
                    .OrderByDescending((Transaction j) => j.Date)
                    .Take(5)
                    .ToListAsync();


                return View();

            }
            catch (Exception ex)
            {
                return StatusCode(500);

            }
        }
    }
    public class DoughnutChartDataItem
    {
        public string CategoryTitleWithIcon { get; set; }
        public decimal Amount { get; set; }
        public string FormattedAmount { get; set; }
    }

    public class SplineChartData
    {
        public string day { get; set; }
        public int income { get; set; }
        public int expense { get; set; }
    }
}