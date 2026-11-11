import {performance} from 'node:perf_hooks';
const base=process.env.BUYER_TEST_URL,password=process.env.BUYER_TEST_PASSWORD;
if(!base||!password||process.env.BUYER_LOAD_ALLOW!=='isolated-database')throw Error('Set test URL/password and BUYER_LOAD_ALLOW=isolated-database');
const users=Number(process.env.BUYER_LOAD_USERS??100), samples=[],errors=[];
const start=performance.now();
await Promise.all(Array.from({length:users},async(_,i)=>{
 const email=`seed.buyer${String(i%12+1).padStart(2,'0')}@example.test`;
 const r=await fetch(base+'/api/auth/login',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({email,password})});
 if(!r.ok){errors.push('login:'+r.status);return}const token=(await r.json()).data.accessToken;
 for(const path of ['/api/products?pageSize=20','/api/cart','/api/notifications']){
  const at=performance.now();const response=await fetch(base+path,{headers:{Authorization:'Bearer '+token}});await response.arrayBuffer();samples.push(performance.now()-at);if(!response.ok)errors.push(path+':'+response.status);
 }
}));
samples.sort((a,b)=>a-b);const summary={virtualClients:users,requests:samples.length+users,errors:errors.length,elapsedMs:Math.round(performance.now()-start),apiP50Ms:Math.round(samples[Math.floor(samples.length*.5)]??0),apiP95Ms:Math.round(samples[Math.floor(samples.length*.95)]??0),apiMaxMs:Math.round(samples.at(-1)??0)};
console.log(JSON.stringify(summary,null,2));if(errors.length)throw Error(errors.slice(0,10).join(', '));
