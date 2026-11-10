import { execFileSync } from 'node:child_process';
const base=process.env.BUYER_TEST_URL, database=process.env.BUYER_TEST_DATABASE,password=process.env.BUYER_TEST_PASSWORD;
if(!base || !password || !/^CloneEbayBuyer_Test_[A-Za-z0-9_]+$/.test(database??''))throw Error('Dedicated test URL, password and database required.');
function sql(q){return execFileSync('sqlcmd',['-S',process.env.BUYER_TEST_SQL_SERVER??'.\\SQLEXPRESS',...(process.env.BUYER_TEST_SQL_USER?['-U',process.env.BUYER_TEST_SQL_USER]:['-E']),'-C','-I','-b','-d',database,'-h','-1','-W','-Q','SET NOCOUNT ON; '+q],{encoding:'utf8'}).trim()}
function check(v,m){if(!v)throw Error(m)}
async function api(p,t,method='GET',body){const r=await fetch(base+p,{method,headers:{'Content-Type':'application/json',...(t?{Authorization:'Bearer '+t}:{})},body:body?JSON.stringify(body):undefined});const j=await r.json();return {status:r.status,data:j.data}}
const tokens=[];for(const n of [11,12])tokens.push((await api('/api/auth/login',null,'POST',{email:`seed.buyer${n}@example.test`,password})).data.accessToken);
const addresses=await Promise.all(tokens.map(async t=>(await api('/api/addresses',t)).data[0].id));
for(let attempt=0;attempt<10;attempt++){
 sql('UPDATE Listing SET quantity=1 WHERE id=40');
 for(const t of tokens){for(const i of (await api('/api/cart',t)).data.items)await api('/api/cart/items/'+i.cartItemId,t,'DELETE');check((await api('/api/cart/items',t,'POST',{listingId:40,quantity:1})).status===200,'add last unit');}
 const race=await Promise.all(tokens.map((t,i)=>api('/api/checkout',t,'POST',{method:'COD',addressId:addresses[i]})));
 check(race.filter(x=>x.status===200).length===1 && race.filter(x=>x.status===409).length===1,'Race must yield 200/409: '+JSON.stringify(race.map(x=>x.status)));
 check(Number(sql('SELECT quantity FROM Listing WHERE id=40'))===0,'stock never negative');
}
console.log('PASS: 10 simultaneous checkout races, stock never negative.');
const t=tokens[1];for(const i of (await api('/api/cart',t)).data.items)await api('/api/cart/items/'+i.cartItemId,t,'DELETE');
sql('UPDATE Listing SET quantity=10 WHERE id=2');await api('/api/cart/items',t,'POST',{listingId:2,quantity:1});
const checkout=(await api('/api/checkout',t,'POST',{method:'PAYPAL',addressId:addresses[1]})).data;
check(checkout?.checkoutGroupId,'checkout created');
try {
 sql(`UPDATE CheckoutGroup SET createdAt=DATEADD(minute,-20,SYSUTCDATETIME()),paymentDeadline=DATEADD(minute,-1,SYSUTCDATETIME()) WHERE id=${checkout.checkoutGroupId}; UPDATE [User] SET accountStatus='LOCKED' WHERE email='seed.buyer12@example.test'`);
 let status='';for(let attempt=0;attempt<9;attempt++){status=sql('SELECT status FROM CheckoutGroup WHERE id='+checkout.checkoutGroupId);if(status==='EXPIRED')break;await new Promise(r=>setTimeout(r,5000));}
 check(status==='EXPIRED','worker expires checkout even if buyer subsequently locked');
 check(Number(sql('SELECT quantity FROM Listing WHERE id=2'))===10,'worker restores inventory');
 await new Promise(r=>setTimeout(r,6000));check(Number(sql('SELECT quantity FROM Listing WHERE id=2'))===10,'next worker tick does not restore twice');
 console.log('PASS: automatic expiry, locked account, exactly-once stock release.');
} finally {sql("UPDATE [User] SET accountStatus='ACTIVE' WHERE email='seed.buyer12@example.test'")}
