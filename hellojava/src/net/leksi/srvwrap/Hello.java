/*
 * To change this license header, choose License Headers in Project Properties.
 * To change this template file, choose Tools | Templates
 * and open the template in the editor.
 */
package net.leksi.srvwrap;

import com.sun.net.httpserver.HttpExchange;
import com.sun.net.httpserver.HttpHandler;
import com.sun.net.httpserver.HttpServer;
import java.io.IOException;
import java.io.OutputStream;
import java.net.InetSocketAddress;

/**
 *
 * @author alexei
 */
public class Hello {
    public static void main(String[] args) throws Exception {
        HttpServer server = HttpServer.create(new InetSocketAddress(8765), 0);
        server.createContext("/", new Handler());
        server.setExecutor(null);
        server.start();
        System.err.println("stop");
    }

    private static class Handler implements HttpHandler {

        public Handler() {
        }

        int serial = 0;
        
        @Override
        public void handle(HttpExchange he) throws IOException {
            String response = "Hello, SrvWrap! (" + (++serial) + ")";
            he.sendResponseHeaders(200, response.length());
            OutputStream os = he.getResponseBody();
            os.write(response.getBytes());
            os.close();
        }
    }
}
