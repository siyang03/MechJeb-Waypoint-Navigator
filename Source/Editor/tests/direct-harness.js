var nextOpen=null,nextSave=null,pickerError=null;
window.showOpenFilePicker=async()=>{if(pickerError)throw pickerError;return [nextOpen];};
window.showSaveFilePicker=async()=>{if(pickerError)throw pickerError;return nextSave;};
function memoryFile(name,text){return {
 name,text,writes:0,fail:false,onWrite:null,aborted:false,
 async getFile(){const text=this.text;return {size:text.length,text:async()=>text};},
 async createWritable(){const file=this;let pending;return {
  async write(text){if(file.fail)throw Error('Permission denied');pending=text;if(file.onWrite)file.onWrite();},
  async close(){file.text=pending;file.writes++;},async abort(){file.aborted=true;}
 };}
};}
