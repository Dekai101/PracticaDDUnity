package com.example.demo.api.model.messages.in.select_mode;

import com.example.demo.api.model.messages.MessageBody;

public class SelectModeMessage_IN extends MessageBody{

    public final static String TYPE = "SELECT_MODE";

    @Override
    public String getMessageType(){
        return TYPE;
    }

    public int mode;
}