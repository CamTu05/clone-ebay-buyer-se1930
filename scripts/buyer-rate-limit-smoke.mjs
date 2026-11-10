// Run against a separately started isolated API configured with LoginPermitLimit=3, PermitLimit=20.
const base=process.env.BUYER_RATE_TEST_URL;if(!base)throw Error('BUYER_RATE_TEST_URL required');
const statuses=[];let retryAfter;
for(let i=0;i<5;i++){const r=await fetch(base+'/api/auth/login',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({email:'unknown@example.test',password:'Incorrect12345'})});statuses.push(r.status);if(r.status===429)retryAfter=r.headers.get('retry-after')}
if(statuses.filter(x=>x===401).length!==3||statuses.filter(x=>x===429).length!==2||!retryAfter)throw Error('Login limiter failed: '+statuses);
const auth=await fetch(base+'/api/orders');if(auth.status!==401)throw Error('Anonymous order access not denied');
const codes=[];for(let i=0;i<22;i++)codes.push((await fetch(base+'/api/products?pageSize=1')).status);
if(!codes.includes(200)||!codes.includes(429))throw Error('API limiter failed: '+codes);
console.log('PASS: login/API rate limits, Retry-After, anonymous order access');
