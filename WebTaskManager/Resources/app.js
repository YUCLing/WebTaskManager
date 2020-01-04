!function () {
    var client = null, wasConnected = false, websocketSupport = window.WebSocket != null;
    if (!websocketSupport) {
        var s = document.createElement("script");
        s.src = "/MD5.js";
        docuemnt.body.appendChild(s);
    }
    var app = new Vue({
        el: "#app",
        data: {
            password: "",
            get isConnected() {
                return (client == null ? false : client.readyState == client.OPEN);
            },
            logged: false,
            procList: [],
            passwordErr: null
        },
        methods: {
            login: function () {
                client.send(JSON.stringify({
                    action: "Verify",
                    password: app.password
                }));
                app.passwordErr = null;
            },
            refresh: function () {
                client.send(JSON.stringify({
                    action: "GetProcesses",
                }));
            },
            end: function (pid) {
                client.send(JSON.stringify({
                    action: "EndProcess",
                    id: pid
                }));
            }
        }
    });
    function connect() {
        app.$forceUpdate();
        client = new WebSocket("ws://" + location.host + "/websocket");
        client.addEventListener("connect", function () {
            wasConnected = true;
            app.$forceUpdate();
        });
        client.addEventListener("close", function () {
            if (wasConnected) {
                mdui.dialog({
                    title: "连接断开",
                    content: "与Web Task Manager的连接已断开，将会自动尝试重新连接",
                    buttons: [
                        {
                            text: "好的"
                        }
                    ]
                });
            }
            app.logged = false;
            setTimeout(connect, 3000);
            app.$forceUpdate();
            wasConnected = false;
        });
        client.addEventListener("message", function (e) {
            try {
                var data = JSON.parse(e.data);
                switch (data["message"]) {
                    case "verify":
                        if (data["success"]) {
                            app.logged = true;
                            client.send(JSON.stringify({
                                action: "GetProcesses"
                            }));
                        } else {
                            app.passwordErr = "密码错误";
                        }
                        app.password = "";
                        break;
                    case "processes":
                        if (data["success"]) {
                            app.procList = data["processes"];
                        }
                        break;
                    case "endprocess":
                        if (data["success"]) {
                            client.send(JSON.stringify({
                                action: "GetProcesses"
                            }));
                        }
                        break;
                }
                app.$forceUpdate();
            } catch {
                return;
            }
        });
    }
    connect();
    setInterval(function () {
        //if (websocketSupport) {
            if (client.readyState == client.OPEN) {
                client.send(JSON.stringify({
                    action: "GetProcesses"
                }));
            }
        /*} else {

        }*/
    }, 1000);
    window.application = app;
}();