package com.mathtutor.controller;
import javafx.application.Application;
import javafx.fxml.FXML;
import javafx.fxml.FXMLLoader;
import javafx.scene.Scene;
import javafx.scene.control.*;
import javafx.scene.layout.VBox;
import javafx.stage.Stage;

import java.io.IOException;
import java.time.LocalDate;
import java.util.HashMap;
import java.util.Map;

public class SchedulePage extends Application {

    @FXML private DatePicker datePicker;
    @FXML private TextField studentNameField;
    @FXML private Button addButton;
    @FXML private ListView<String> listView;

    private Map<LocalDate, String> schedule = new HashMap<>();

    private void addRecord(LocalDate date, String studentName) {
        schedule.put(date, studentName);
    }

    private String getRecordForDate(LocalDate date) {
        return schedule.getOrDefault(date, "Нет записи на этот день");
    }

    @FXML
    private void initialize() {
        // При инициализации установим текущую дату
        datePicker.setValue(LocalDate.now());

        // Добавление записи по кнопке
        addButton.setOnAction(event -> {
            LocalDate selectedDate = datePicker.getValue();
            String studentName = studentNameField.getText();
            if (!studentName.isEmpty()) {
                addRecord(selectedDate, studentName);
                studentNameField.clear();
                // Обновляем отображение
                listView.getItems().setAll(getRecordForDate(selectedDate));
            }
        });

        // Обработчик выбора даты
        datePicker.setOnAction(event -> {
            LocalDate selectedDate = datePicker.getValue();
            listView.getItems().setAll(getRecordForDate(selectedDate));
        });
    }

    @Override
    public void start(Stage stage) throws IOException {
        FXMLLoader loader = new FXMLLoader(getClass().getResource("resources/schedule.fxml"));
        loader.setController(this);  // Устанавливаем текущий класс как контроллер
        VBox vbox = loader.load();

        Scene scene = new Scene(vbox, 400, 300);
        stage.setTitle("Расписание");
        stage.setScene(scene);
        stage.show();
    }

    public static void main(String[] args) {
        launch(args);
    }
}
