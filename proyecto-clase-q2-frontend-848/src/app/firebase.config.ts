import { initializeApp } from 'firebase/app';
import { getAuth } from 'firebase/auth';

const firebaseConfig = {
  apiKey: "AIzaSyBcnd2811WkVkGosKbRm_bS90sSOPOxQ0Y",
  authDomain: "donacerca.firebaseapp.com",
  projectId: "donacerca",
  storageBucket: "donacerca.firebasestorage.app",
  messagingSenderId: "960106121563",
  appId: "1:960106121563:web:f475e544cf484a195ef27e"
};

export const firebaseApp = initializeApp(firebaseConfig);
export const firebaseAuth = getAuth(firebaseApp);
