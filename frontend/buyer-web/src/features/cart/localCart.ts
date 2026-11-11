import { useSyncExternalStore } from 'react'
import type { ProductSummary } from '../catalog/types'
import { apiClient } from '../../shared/api/apiClient'
export type LocalItem=ProductSummary & {quantity:number}
const key='buyer.guest-cart.v1';const listeners=new Set<()=>void>()
function read():LocalItem[]{try{const raw=JSON.parse(localStorage.getItem(key)??'[]');return Array.isArray(raw)?raw.filter(x=>Number.isSafeInteger(x.listingId)&&x.listingId>0&&Number.isInteger(x.quantity)&&x.quantity>0&&x.quantity<=1000&&typeof x.name==='string').slice(0,100):[]}catch{return []}}
let items=read();let message='';
function emit(){listeners.forEach(f=>f())}
function save(next:LocalItem[]){items=next;try{localStorage.setItem(key,JSON.stringify(next))}catch{message='Browser storage unavailable. Keep this tab open.'}emit()}
export function addLocal(product:ProductSummary){const current=items.find(x=>x.listingId===product.listingId);save(current?items.map(x=>x.listingId===product.listingId?{...x,quantity:Math.min(1000,x.quantity+1)}:x):[...items,{...product,quantity:1}])}
export function clearLocal(){save([])}
export async function mergeLocal(){
 const snapshot=items;if(!snapshot.length)return;
 const payload=snapshot.map(x=>({listingId:x.listingId,quantity:x.quantity}));const signature=JSON.stringify(payload);
 let op:string;try{const old=JSON.parse(localStorage.getItem('buyer.merge-key')??'null');op=old?.signature===signature?old.key:crypto.randomUUID();localStorage.setItem('buyer.merge-key',JSON.stringify({signature,key:op}))}catch{op=crypto.randomUUID()}
 try{const r=await apiClient.post('/api/cart/merge',{key:op,items:payload});const results:rResult[]=r.data.data.items;const merged=new Set(results.filter(x=>x.merged).map(x=>x.listingId));save(items.flatMap(x=>{if(!merged.has(x.listingId))return [x];const quantity=x.quantity-(snapshot.find(y=>y.listingId===x.listingId)?.quantity??0);return quantity>0?[{...x,quantity}]:[]}));try{localStorage.removeItem('buyer.merge-key')}catch{/* request was applied */}message=results.some(x=>!x.merged)?'Some guest items could not be merged. Review the remaining guest cart.':'';emit()}catch{message='Guest cart could not be merged. Retry from your cart.';items=[...items];emit()}
}
type rResult={listingId:number;merged:boolean}
window.addEventListener('storage',e=>{if(e.key===key){items=read();emit()}})
export function useLocalCart(){const saved=useSyncExternalStore(f=>{listeners.add(f);return()=>{listeners.delete(f)}},()=>items);return {items:saved,message,remove:(id:number)=>save(items.filter(x=>x.listingId!==id)),update:(id:number,q:number)=>{if(Number.isInteger(q)&&q>0&&q<=1000)save(items.map(x=>x.listingId===id?{...x,quantity:q}:x))}}}
