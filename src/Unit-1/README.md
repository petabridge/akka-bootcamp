# Тренировочный центр Akka.NET  - Блок №1: Начало работы с Akka.NET

![Akka.NET logo](../../images/akka_net_logo.png)

In Unit 1, we will learn the fundamentals of how the actor model and Akka.NET work.
В блоке номер 1, мы изучм основы работы Akka.NET и модели акторов.

## Concepts you'll learn

## Концепции, с которыми вы познакомитесь.

*NIX systems have the `tail` command built-in to monitor changes to a file (such as tailing log files), whereas Windows does not. We will recreate `tail` for Windows, and use the process to learn the fundamentals.


В состав *NIX  входит встроенная команда `tail`, которая позволяет отслеживать изменения в файле (например наблюдать за постоянно растущим  файлом логов). Мы создадим аналог команды `tail` для Windows и одновременно получим практические навыки работы основами.

In Unit 1 you will learn the following:
из блоке №1 вы  сможете узнать следующее:

1. How to create your own `ActorSystem` and actors;
1. Как создавать актором и `ActorSystem5`

2. How to send messages actors and how to handle different types of messages;
2. Как посылать собщения актоам и как обрабатывать различные типы сообщений

3. How to use `Props` and `IActorRef`s to build loosely coupled systems.
3. Как исполользовать `Props` и `IActorRef`ы для построения систем со слабой связностью.

4. How to use actor paths, addresses, and `ActorSelection` to send messages to actors.
4. Как использовать пути акторов, адреса и `ActorSelection` для того, чтобы посылать сообщения нужным акторам.

5. How to create child actors and actor hierarchies, and how to supervise children with `SupervisionStrategy`.
5. Как создавать дочерних акторов и иерархии акторов, и как контролировать детей при помощи `SupervisionStrategy`.

6. How to use the Actor lifecycle to control actor startup, shutdown, and restart behavior.
6. Как можно использовать жизненный цикл актора для более тонкого контроля над запуском, остановкой и перезапуском актора.


## Using Xamarin?
## Используете Xamarin?

Since Unit 1 relies heavily on the console, you'll need to make a small tweaks before beginning. You need to set up your `WinTail` project file (not the solution) to use an **external console**.

Поскольку блок 1 активно использует консольное приложение, вам необходимо провести небольшую предварительную работу.
 Вам придется настроить проект `WinTail`  (не solution)  на использование  **внешней консоли**.


To set this up:
Для того, чтобы это сделать


1. Click on the `WinTail` project (not the solution)
1. Кликните на проект `WinTail` (не solution)

2. Navigate to `Project > WinTail Options` in the menu
2. В меню нажмите `Project > WinTail Options`

3. Inside `WinTail Options`, navigate to `Run > General`
3. Внутри `WinTail Options`, перейдите к `Run > General`

4. Select `Run on external console`
4. Выберите опцию `Run on external console`


5. Click `OK`
5. Кликните `OK`

Here is a demonstration of how to set it up:
![Configure Xamarin to use external console](../../images/xamarin.gif)
Ниже приведен пример правильной настройки проекта:
![Configure Xamarin to use external console](../../images/xamarin.gif)


## Table of Contents
## Оглавление

1. **[Lesson 1 - Actors and the `ActorSystem`](lesson1/)**
1. **[Урок 1 - Акторы и `ActorSystem`](lesson1/)**

2. **[Lesson 2 - Defining and Handling Messages](lesson2/)**
2. **[Урок 2 - Создание и обработка сообщений](lesson2/)**

3. **[Lesson 3: Using `Props` and `IActorRef`s](lesson3/)**
3. **[Урок 3: Используем `Props` и `IActorRef`s](lesson3/)**


4. **[Lesson 4: Child Actors, Hierarchies, and Supervision](lesson4/)**
4. **[Урок 4: Дочерние акторы, иерархии и супервизоры](lesson4/)**


5. **[Lesson 5: Looking up actors by address with `ActorSelection`](lesson5/)**
5. **[Урок 5: Ищем акторов по адресу при помощи `ActorSelection`](lesson5/)**

6. **[Lesson 6: The Actor Lifecycle](lesson6/)**
6. **[Урок 6: Жизненный цикл актора](lesson6/)**


## Get Started
## Начинаем

To get started, [go to the /DoThis/ folder](DoThis/) and open `WinTail.sln`.

Прежде всего, [перейдите в папку  /DoThis/ ](DoThis/) и откройте `WinTail.sln`.

Потом идите в [Lesson 1](lesson1/).
