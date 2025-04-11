package com.mathtutor.controller;

import javafx.fxml.FXML;
import javafx.scene.control.ListView;

import java.util.Arrays;
import java.util.List;

public class PaymentsController {

    @FXML
    private ListView<String> paidListView;

    @FXML
    private ListView<String> unpaidListView;

    @FXML
    private void initialize() {
        // Здесь можно подгрузить данные из БД. Пока — статично:
        List<String> paidStudents = Arrays.asList("Антон Аксенов", "Рината Киласханова", "Илья Фисина");
        List<String> unpaidStudents = Arrays.asList("Кирилл Иванов", "Светлана Морозова");

        paidListView.getItems().addAll(paidStudents);
        unpaidListView.getItems().addAll(unpaidStudents);
    }
}
