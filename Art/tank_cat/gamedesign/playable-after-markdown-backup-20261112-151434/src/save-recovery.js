import {SAVE_KEY,BACKUP_KEY} from './meta/economy.js';
export function showSaveRecovery(error){
 const panel=document.querySelector('.intro-panel');document.querySelector('#start').hidden=true;
 const message=document.querySelector('#loading-message');message.textContent=`Không thể mở hồ sơ: ${error.message}`;
 const actions=document.createElement('div');actions.className='controls-help';
 const download=document.createElement('button');download.className='primary';download.textContent='XUẤT DỮ LIỆU ĐỂ KHÔI PHỤC';download.onclick=()=>{let main,backup;try{main=localStorage.getItem(SAVE_KEY);backup=localStorage.getItem(BACKUP_KEY);}catch{}const url=URL.createObjectURL(new Blob([JSON.stringify({main,backup})],{type:'application/json'})),link=document.createElement('a');link.href=url;link.download='meow-save-recovery.json';link.click();setTimeout(()=>URL.revokeObjectURL(url),1000);};
 const reset=document.createElement('button');reset.className='primary';reset.textContent='TẠO HỒ SƠ MỚI';reset.onclick=()=>{if(confirm('Xóa hồ sơ lỗi và bản dự phòng trên trình duyệt này để chơi mới? Hãy xuất dữ liệu trước nếu muốn giữ lại.')){try{localStorage.removeItem(SAVE_KEY);localStorage.removeItem(BACKUP_KEY);location.reload();}catch(e){message.textContent=`Trình duyệt không cho lưu dữ liệu: ${e.message}`;}}};
 actions.append(download,reset);panel.append(actions);
}
