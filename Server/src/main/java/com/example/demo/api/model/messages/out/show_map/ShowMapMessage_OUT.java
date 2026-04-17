package com.example.demo.api.model.messages.out.show_map;

import java.util.List;

import com.example.demo.api.model.Room;
import com.example.demo.api.model.messages.MessageBody;

public class ShowMapMessage_OUT extends MessageBody{

    public final static String TYPE = "SHOW_MAP";
    

    @Override
    public String getMessageType(){
        return TYPE;
    }

    public List<Room> rooms;
}
