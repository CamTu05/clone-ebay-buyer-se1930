import {useEffect,useRef,useState} from 'react'
import {Link,useNavigate} from 'react-router-dom'
import {useAuth} from './useAuth'
import {buyerGet,buyerError} from '../buyerApi'
import {apiClient} from '../../shared/api/apiClient'
export function GoogleSignIn(){const [enabled,setEnabled]=useState(false);useEffect(()=>{const c=new AbortController();buyerGet<{google:boolean}>('/api/auth/providers',c.signal).then(x=>setEnabled(x.google)).catch(()=>{});return()=>c.abort()},[]);return enabled?<a className="buyer-pill secondary" href={(apiClient.defaults.baseURL??'')+'/api/auth/google'}>Continue with Google</a>:null}
export function ExternalLoginPage(){const {completeExternal}=useAuth();const navigate=useNavigate();const started=useRef(false);const [error,setError]=useState('');useEffect(()=>{if(started.current)return;started.current=true;const code=new URLSearchParams(window.location.hash.slice(1)).get('code');history.replaceState(null,'',window.location.pathname);if(!code){void Promise.resolve().then(()=>setError('Missing sign-in code.'));return}void completeExternal(code).then(()=>navigate('/')).catch(e=>setError(buyerError(e)))},[completeExternal,navigate]);return <section className="buyer-login"><h1>Completing sign-in…</h1>{error&&<p role="alert">{error} <Link to="/login">Try again</Link></p>}</section>}
