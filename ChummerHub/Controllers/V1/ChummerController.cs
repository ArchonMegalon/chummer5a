/*  This file is part of Chummer5a.
 *
 *  Chummer5a is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  Chummer5a is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with Chummer5a.  If not, see <http://www.gnu.org/licenses/>.
 *
 *  You can obtain the full source code for Chummer5a at
 *  https://github.com/chummer5a/chummer5a
 */
using ChummerHub.Data;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Text;
using System.Text.Encodings.Web;
using ChummerHub.Models.V1;
using Newtonsoft.Json;
using ChummerHub.API;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace ChummerHub.Controllers.V1
{
    [Route("[action]/{Hash}")]
    [ApiController]
    [EnableCors("AllowOrigin")]
    [ApiVersion("1.0")]
    [ControllerName("Chummer")]
    [Authorize(Roles = API.Authorization.Constants.UserRolePublicAccess, AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme + "," + CookieAuthenticationDefaults.AuthenticationScheme)]
    public class ChummerController : Controller
    {
        private readonly ILogger _logger;
        private readonly TelemetryClient tc;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        public ApplicationDbContext _context;

        public ChummerController(ApplicationDbContext context,
            ILogger<ChummerController> logger,
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            TelemetryClient telemetry)
        {
            _context = context;
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
            tc = telemetry;
        }

        [HttpGet]
        [EnableCors("AllowAllOrigins")]
        [Swashbuckle.AspNetCore.Annotations.SwaggerResponse((int)HttpStatusCode.Redirect)]
        [Swashbuckle.AspNetCore.Annotations.SwaggerResponse((int)HttpStatusCode.OK)]
        [Swashbuckle.AspNetCore.Annotations.SwaggerResponse((int)HttpStatusCode.RedirectKeepVerb)]
        [Swashbuckle.AspNetCore.Annotations.SwaggerResponse((int)(HttpStatusCode.PermanentRedirect))]
        [Swashbuckle.AspNetCore.Annotations.SwaggerResponse((int)HttpStatusCode.NotFound)]
        [Swashbuckle.AspNetCore.Annotations.SwaggerOperation("ChummerO")]
        public IActionResult O([FromRoute] string Hash, string open)
        {
            try
            {
                if (string.IsNullOrEmpty(Hash))
                    throw new ArgumentException("hash is empty: " + Hash);
                if (!_context.SINners.Any(a => a.Hash == Hash))
                {
                    foreach (var nullSinner in _context.SINners.Where(a => string.IsNullOrEmpty(a.Hash) || a.Hash == "25943ECC"))
                    {
                        string message = "Saving Hash for SINner " + nullSinner.Id + ": " + nullSinner.MyHash;
                        TraceTelemetry tt = new TraceTelemetry(message, SeverityLevel.Verbose);
                        tc?.TrackTrace(tt);
                    }
                }
                var sinner = _context.SINners.FirstOrDefault(a => a.Hash == Hash);
                _context.SaveChanges();
#if DEBUG
                if (Debugger.IsAttached)
                    sinner = _context.SINners.FirstOrDefault();
#endif
                if (sinner != null)
                {
                    string transactionId = Guid.NewGuid().ToString("N");
                    string chummerUrl = "chummer://plugin:SINners:Load:" + sinner.Id + ":" + transactionId;
                    string postbackUrl = "https://shadowsprawl.com/api/chummer/upload";
                    sinner.LastDownload = DateTime.Now;
                    _context.SaveChanges();
                    string mypath = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
                    StringBuilder sb = new StringBuilder("<html>")
                        .AppendFormat(@"<body onload='document.forms[""form""].submit()'>")
                        .AppendFormat("<form name='form' action='{0}' method='post'>", EscapeHiddenInputValue(postbackUrl))
                        .AppendFormat("<input type='hidden' name='guid' value='{0}'>", EscapeHiddenInputValue(sinner.Id.ToString()))
                        .AppendFormat("<input type='hidden' name='Environment' value='{0}'>", EscapeHiddenInputValue(mypath))
                        .AppendFormat("<input type='hidden' name='CharName' value='{0}'>", EscapeHiddenInputValue(sinner.Alias));

                    string escapestr = sinner.DownloadUrl;
                    if (Uri.TryCreate(sinner.DownloadUrl, UriKind.Absolute, out Uri escape))
                    {
                        escapestr = $"{escape.Scheme}://{escape.Host}{escape.AbsolutePath}" + Uri.EscapeDataString(escape.Query);
                    }

                    sb.AppendFormat("<input type='hidden' name='DownloadUrl' value='{0}'>", EscapeHiddenInputValue(escapestr));

                    string urlcallback = "https://shadowsprawl.com/character/status/" + transactionId;
                    string chummeruri = chummerUrl + ":" + Uri.EscapeDataString(urlcallback);
                    sb.AppendFormat("<input type='hidden' name='ChummerUrl' value='{0}'>", EscapeHiddenInputValue(chummeruri))
                        .AppendFormat("<input type='hidden' name='TransactionId' value='{0}'>", EscapeHiddenInputValue(transactionId))
                        .AppendFormat("<input type='hidden' name='StatusCallback' value='{0}'>", EscapeHiddenInputValue(urlcallback))
                        .AppendFormat("<input type='hidden' name='UploadDateTime' value='{0}'>", EscapeHiddenInputValue(sinner.UploadDateTime.ToString()))
                        .AppendFormat("<input type='hidden' name='OpenChummer' value='{0}'>", EscapeHiddenInputValue(open));
                    // Other params go here
                    sb.Append("</form></body></html>");
                    string strBody = sb.ToString();
                    var contentresult = new ContentResult
                    {
                        ContentType = "text/html",
                        StatusCode = (int)HttpStatusCode.OK,
                        Content = strBody
                    };
                    if (tc != null)
                    {
                        tc.TrackTrace("Form generated: " + strBody, SeverityLevel.Information);
                        string msg = "Redirecting Hash " + Hash + " to Shadowsprawl with there parameters: "
                                     + Environment.NewLine + Environment.NewLine;
                        msg += contentresult;
                        TraceTelemetry tt = new TraceTelemetry(msg, SeverityLevel.Verbose);
                        tc.TrackTrace(tt);
                    }
                    return contentresult;
                }
                return NotFound("Hash not found.");
            }
            catch (Exception e)
            {
                tc?.TrackException(e);
                throw;
            }
        }

        [HttpGet]
        [EnableCors("AllowAllOrigins")]
        [Swashbuckle.AspNetCore.Annotations.SwaggerResponse((int)HttpStatusCode.Redirect)]
        [Swashbuckle.AspNetCore.Annotations.SwaggerResponse((int)HttpStatusCode.OK)]
        [Swashbuckle.AspNetCore.Annotations.SwaggerResponse((int)HttpStatusCode.RedirectKeepVerb)]
        [Swashbuckle.AspNetCore.Annotations.SwaggerResponse((int)(HttpStatusCode.PermanentRedirect))]
        [Swashbuckle.AspNetCore.Annotations.SwaggerResponse((int)HttpStatusCode.NotFound)]
        [Swashbuckle.AspNetCore.Annotations.SwaggerOperation("GroupO")]
        public async Task<IActionResult> G([FromRoute] string Hash, string open)
        {
            try
            {
                if (string.IsNullOrEmpty(Hash))
                    throw new ArgumentException("hash is empty: " + Hash);
                if (!_context.SINnerGroups.Include(a => a.MyGroups).Any(a => a.Hash == Hash))
                {
                    foreach (var nullSinner in _context.SINnerGroups.Where(a => string.IsNullOrEmpty(a.Hash) || a.Hash == "25943ECC"))
                    {
                        string message = "Saving Hash for SINner " + nullSinner.Id + ": " + nullSinner.MyHash;
                        TraceTelemetry tt = new TraceTelemetry(message, SeverityLevel.Verbose);
                        tc?.TrackTrace(tt);
                    }
                }
                var sgi = _context.SINnerGroups.FirstOrDefault(a => a.Hash == Hash);
                _context.SaveChanges();
#if DEBUG
                if (Debugger.IsAttached)
                    sgi = _context.SINnerGroups.FirstOrDefault();
#endif
                if (sgi != null)
                {
                    var user = await _signInManager.UserManager.GetUserAsync(User).ConfigureAwait(false);
                    SINnerSearchGroup sg = new SINnerSearchGroup(sgi, user);
                    string transactionId = Guid.NewGuid().ToString("N");
                    string chummerUrl = "chummer://plugin:SINners:Load:" + sg.Id + ":" + transactionId;

                    string postbackUrl = "https://shadowsprawl.com/api/chummer/upload";
                    _context.SaveChanges();
                    string mypath = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
                    StringBuilder sb = new StringBuilder("<html>")
                        .AppendFormat(@"<body onload='document.forms[""form""].submit()'>")
                        .AppendFormat("<form name='form' action='{0}' method='post'>", EscapeHiddenInputValue(postbackUrl))
                        .AppendFormat("<input type='hidden' name='guid' value='{0}'>", EscapeHiddenInputValue(sg.Id.ToString()))
                        .AppendFormat("<input type='hidden' name='Environment' value='{0}'>", EscapeHiddenInputValue(mypath))
                        .AppendFormat("<input type='hidden' name='GroupName' value='{0}'>", EscapeHiddenInputValue(sg.Groupname))
                        .AppendFormat("<input type='hidden' name='HasPassword' value='{0}'>", EscapeHiddenInputValue(sg.HasPassword.ToString()))
                        .AppendFormat("<input type='hidden' name='Description' value='{0}'>", EscapeHiddenInputValue(sg.Description))
                        .AppendFormat("<input type='hidden' name='IsPublic' value='{0}'>", EscapeHiddenInputValue(sg.IsPublic.ToString()))
                        .AppendFormat("<input type='hidden' name='Language' value='{0}'>", EscapeHiddenInputValue(sg.Language));
                    string urlcallback = "https://shadowsprawl.com/character/status/" + transactionId;
                    string chummeruri = chummerUrl + ":" + Uri.EscapeDataString(urlcallback);
                    sb.AppendFormat("<input type='hidden' name='ChummerUrl' value='{0}'>", EscapeHiddenInputValue(chummeruri))
                        .AppendFormat("<input type='hidden' name='TransactionId' value='{0}'>", EscapeHiddenInputValue(transactionId))
                        .AppendFormat("<input type='hidden' name='StatusCallback' value='{0}'>", EscapeHiddenInputValue(urlcallback))
                        .AppendFormat("<input type='hidden' name='OpenChummer' value='{0}'>", EscapeHiddenInputValue(open));

                    // Other params go here
                    var members = await sg.GetGroupMembers(_context, false).ConfigureAwait(false);
                    var json = JsonConvert.SerializeObject(members, Formatting.Indented);
                    sb.AppendFormat("<input type='hidden' name='Members' value='{0}'>", EscapeHiddenInputValue(json));

                    var jsonsubgroups = JsonConvert.SerializeObject(sg.MyGroups);
                    sb.AppendFormat("<input type='hidden' name='SubGroups' value='{0}'>", EscapeHiddenInputValue(jsonsubgroups));

                    sb.Append("</form></body></html>");
                    string strBody = sb.ToString();
                    var contentresult = new ContentResult
                    {
                        ContentType = "text/html",
                        StatusCode = (int)HttpStatusCode.OK,
                        Content = strBody
                    };
                    if (tc != null)
                    {
                        tc.TrackTrace("Form generated: " + strBody, SeverityLevel.Information);
                        string msg = "Redirecting Hash " + Hash + " to Shadowsprawl with there parameters: "
                                     + Environment.NewLine + Environment.NewLine;
                        msg += contentresult;
                        TraceTelemetry tt = new TraceTelemetry(msg, SeverityLevel.Verbose);
                        tc.TrackTrace(tt);
                    }
                    return contentresult;
                }
                return NotFound("Hash not found.");
            }
            catch (Exception e)
            {
                tc?.TrackException(e);
                throw;
            }
        }


        [HttpGet]
        [EnableCors("AllowAllOrigins")]
        [Swashbuckle.AspNetCore.Annotations.SwaggerResponse((int)HttpStatusCode.Redirect)]
        [Swashbuckle.AspNetCore.Annotations.SwaggerResponse((int)HttpStatusCode.NotFound)]
        [Swashbuckle.AspNetCore.Annotations.SwaggerOperation("ChummerOpen")]
        public async Task<ActionResult> Open([FromRoute] string Hash)
        {
            try
            {
                if (string.IsNullOrEmpty(Hash))
                    throw new ArgumentException("hash is empty: " + Hash);
                if (!_context.SINners.Any(a => a.Hash == Hash))
                {
                    foreach (var nullSinner in _context.SINners.Where(a => string.IsNullOrEmpty(a.Hash) || a.Hash == "25943ECC"))
                    {
                        string message = "Saving Hash for SINner " + nullSinner.Id + ": " + nullSinner.MyHash;
                        TraceTelemetry tt = new TraceTelemetry(message, SeverityLevel.Verbose);
                        tc?.TrackTrace(tt);
                    }
                }

                var sinner = await _context.SINners.FirstOrDefaultAsync(a => a.Hash == Hash);
                await _context.SaveChangesAsync();
                if (sinner != null)
                {
                    string url = "chummer://plugin:SINners:Load:" + sinner.Id;
                    sinner.LastDownload = DateTime.Now;
                    await _context.SaveChangesAsync();
                    //Redirect(string url);
                    //RedirectPermanent(string url);
                    //RedirectPermanentPreserveMethod(string url);
                    //RedirectPreserveMethod(string url);
                    return RedirectPreserveMethod(url);
                }

                return NotFound("Could not find SINner with Hash " + Hash);
            }
            catch (Exception e)
            {
                tc?.TrackException(e);
                throw;
            }
        }

        internal static string EscapeHiddenInputValue(string value)
        {
            return HtmlEncoder.Default.Encode(value ?? string.Empty);
        }
    }
}
