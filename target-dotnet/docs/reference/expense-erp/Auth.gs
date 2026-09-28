const AUTH={OTP_TTL:600,SESSION_TTL:21600,RESEND_TTL:60,MAX_ATTEMPTS:5};
function normEmail_(v){return String(v||'').trim().toLowerCase()}
function hash_(v){return Utilities.computeDigest(Utilities.DigestAlgorithm.SHA_256,String(v),Utilities.Charset.UTF_8).map(b=>('0'+((b+256)%256).toString(16)).slice(-2)).join('')}
function randomOtp_(){const b=Utilities.computeDigest(Utilities.DigestAlgorithm.SHA_256,Utilities.getUuid());let n=0;for(let i=0;i<4;i++)n=(n*256+((b[i]+256)%256))>>>0;return String(n%1000000).padStart(6,'0')}
function safeEq_(a,b){a=String(a);b=String(b);if(a.length!==b.length)return false;let x=0;for(let i=0;i<a.length;i++)x|=a.charCodeAt(i)^b.charCodeAt(i);return x===0}
function roleUser_(email){const r=objs_(S.ROLE).find(x=>normEmail_(x.Email)===email&&String(x['Active?']||'Yes').toLowerCase()!=='no');if(!r)return null;const role=String(r.Role||'EMPLOYEE').toUpperCase();if(!ROLES[role])throw Error('Invalid role for authorised user');return{email,employeeId:String(r['Employee ID']||''),name:String(r['Employee Name']||email),role,level:ROLES[role],canReview:ROLES[role]>=ROLES.ACCOUNTS,canApprove:ROLES[role]>=ROLES.MD}}
function loginAudit_(email,event,result,agent){try{write_(S.LOGIN,[Utilities.getUuid(),normEmail_(email),event,result,String(agent||'').slice(0,250),new Date()])}catch(e){console.error(e)}}
function requestOtp(email,agent){email=normEmail_(email);if(!/^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(email))return{ok:true,message:'If this email is authorised, an OTP will be sent.'};const cache=CacheService.getScriptCache(),ek=hash_(email);if(cache.get('cool:'+ek))return{ok:true,message:'Please wait one minute before requesting another OTP.'};const u=roleUser_(email);cache.put('cool:'+ek,'1',AUTH.RESEND_TTL);if(!u){loginAudit_(email,'OTP_REQUEST','DENIED',agent);return{ok:true,message:'If this email is authorised, an OTP will be sent.'}}const otp=randomOtp_(),nonce=Utilities.getUuid();cache.put('otp:'+ek,JSON.stringify({hash:hash_(otp+'|'+nonce),nonce,attempts:0}),AUTH.OTP_TTL);MailApp.sendEmail({to:email,subject:'SESS Expense ERP login OTP',htmlBody:'<p>Your SESS Expense ERP OTP is:</p><p style="font-size:28px;font-weight:bold;letter-spacing:5px">'+otp+'</p><p>This code expires in 10 minutes. Do not share it.</p>',name:'SESS Expense ERP'});loginAudit_(email,'OTP_REQUEST','SENT',agent);return{ok:true,message:'If this email is authorised, an OTP will be sent.',expiresMinutes:10}}
function verifyOtp(email,otp,agent){email=normEmail_(email);otp=String(otp||'').replace(/\D/g,'').slice(0,6);const u=roleUser_(email),cache=CacheService.getScriptCache(),ek=hash_(email),raw=cache.get('otp:'+ek);if(!u||!raw){loginAudit_(email,'OTP_VERIFY','FAILED_OR_EXPIRED',agent);throw Error('Invalid or expired OTP')}const rec=JSON.parse(raw);if(rec.attempts>=AUTH.MAX_ATTEMPTS){cache.remove('otp:'+ek);loginAudit_(email,'OTP_VERIFY','LOCKED',agent);throw Error('Too many attempts. Request a new OTP.')}if(!safeEq_(hash_(otp+'|'+rec.nonce),rec.hash)){rec.attempts++;cache.put('otp:'+ek,JSON.stringify(rec),AUTH.OTP_TTL);loginAudit_(email,'OTP_VERIFY','FAILED',agent);throw Error('Invalid or expired OTP')}cache.remove('otp:'+ek);const token=Utilities.getUuid()+Utilities.getUuid(),key='sess:'+hash_(token);cache.put(key,JSON.stringify(u),AUTH.SESSION_TTL);loginAudit_(email,'LOGIN','SUCCESS',agent);return{ok:true,token,user:u,expiresHours:6}}
function auth_(token){const raw=CacheService.getScriptCache().get('sess:'+hash_(token||''));if(!raw)throw Error('AUTH_REQUIRED');const u=JSON.parse(raw),live=roleUser_(u.email);if(!live)throw Error('ACCESS_REVOKED');AUTH_CONTEXT=live;return live}
function logout(token,agent){let email='';try{email=auth_(token).email}catch(e){}CacheService.getScriptCache().remove('sess:'+hash_(token||''));AUTH_CONTEXT=null;loginAudit_(email,'LOGOUT','SUCCESS',agent);return{ok:true}}
function v4Bootstrap(t){auth_(t);return bootstrap()}
function v4SuggestLocations(t,q){auth_(t);return suggestLocations(q)}
function v4SaveExpense(t,f){auth_(t);return saveExpenseV41_(f)}
function v4SaveTrip(t,f){auth_(t);return saveTrip(f)}
function v4SaveAdvance(t,f){auth_(t);return saveAdvance(f)}
function v4SaveSettlement(t,f){auth_(t);return saveSettlement(f)}
function v4PendingAll(t){auth_(t);return pendingAll()}
function v4ReviewAny(t,kind,id,action,accepted,remarks){auth_(t);return reviewAny(kind,id,action,accepted,remarks)}
function v4Report(t,f){auth_(t);return reportV3(f)}
function v4BackupUrl(t){auth_(t);return backupUrl()}
function v41PlaceSuggestions(t,q){auth_(t);return placeSuggestionsV41_(q)}
function v41PreviewRoute(t,points){auth_(t);return previewRouteV41_(points)}
function v41SaveTravel(t,f){auth_(t);return saveTravelV41_(f)}
function v41PendingAll(t){auth_(t);return pendingAllV41_()}
function v41ReviewAny(t,kind,id,action,km,remarks){auth_(t);return kind==='TRAVEL'?reviewTravelV41_(id,action,km,remarks):reviewAny(kind,id,action,km,remarks)}
function v41Report(t,f){auth_(t);return reportV41_(f)}
function v41SaveObligation(t,f){auth_(t);return saveObligationV41_(f)}
