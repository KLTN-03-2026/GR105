export function initShortcut(dotNetHelper) {
    console.log("Admin shortcut module loaded! Listening for Shift + D + O + P");
    const pressed = new Set();
    
    const keydownHandler = (e) => {
        if (e.key) {
            pressed.add(e.key.toLowerCase());
            // console.log("Keys currently pressed:", Array.from(pressed)); // Uncomment to debug ghosting
        }
        
        // Kiểm tra xem phím Shift và các phím d, o, p có đang được giữ không
        if (e.shiftKey && pressed.has('d') && pressed.has('o') && pressed.has('p')) {
            console.log("Shortcut match! Invoking ToggleAdminMode in C#...");
            dotNetHelper.invokeMethodAsync('ToggleAdminMode')
                .then(() => console.log("Toggle successful"))
                .catch(err => console.error("Toggle failed", err));
                
            pressed.clear(); // Reset trạng thái để tránh bị gọi nhiều lần liên tục
        }
    };
    
    const keyupHandler = (e) => {
        if (e.key) {
            pressed.delete(e.key.toLowerCase());
        }
    };
    
    window.addEventListener('keydown', keydownHandler);
    window.addEventListener('keyup', keyupHandler);

    return {
        dispose: () => {
            console.log("Admin shortcut module disposed");
            window.removeEventListener('keydown', keydownHandler);
            window.removeEventListener('keyup', keyupHandler);
        }
    };
}